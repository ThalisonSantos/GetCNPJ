using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Exceptions;
using GetCNPJ.Interfaces;
using GetCNPJ.Models;
using GetCNPJ.Validation;

namespace GetCNPJ.Providers.Base
{
    /// <summary>
    /// Classe base para provedores de dados de CNPJ
    /// </summary>
    public abstract class CnpjProviderBase : ICnpjProvider
    {
        protected readonly HttpClient _httpClient;
        protected readonly IRateLimiter _rateLimiter;

        /// <inheritdoc/>
        public abstract string ProviderName { get; }

        /// <inheritdoc/>
        public abstract int Priority { get; }

        /// <summary>
        /// URL base da API
        /// </summary>
        protected abstract string BaseUrl { get; }

        protected CnpjProviderBase(HttpClient httpClient, IRateLimiter rateLimiter)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        }

        /// <inheritdoc/>
        public async Task<CnpjData> GetCnpjDataAsync(string cnpj, CancellationToken cancellationToken = default)
        {
            try
            {
                // Valida e normaliza o CNPJ
                cnpj = ValidateAndNormalizeCnpj(cnpj);

                if (_rateLimiter is IAtomicRateLimiter atomicRateLimiter)
                {
                    // A aquisição já registra a requisição; não registrar novamente.
                    await atomicRateLimiter.AcquireAsync(ProviderName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Compatibilidade com implementações externas do contrato original.
                    await _rateLimiter.WaitIfNeededAsync(ProviderName, cancellationToken).ConfigureAwait(false);
                    _rateLimiter.RecordRequest(ProviderName);
                }

                // Faz a requisição
                var data = await FetchDataAsync(cnpj, cancellationToken).ConfigureAwait(false);

                if (data != null)
                {
                    data.Provedor = ProviderName;
                }

                return data;
            }
            catch (InvalidCnpjException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProviderException(ProviderName, $"Erro ao consultar CNPJ: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> IsAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(BaseUrl).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Método abstrato para buscar dados do provedor específico
        /// </summary>
        protected abstract Task<CnpjData> FetchDataAsync(string cnpj, CancellationToken cancellationToken);

        /// <summary>
        /// Valida e normaliza um CNPJ numérico ou alfanumérico (remove a máscara padrão).
        /// </summary>
        protected string ValidateAndNormalizeCnpj(string cnpj)
        {
            return CnpjValidator.ValidateAndNormalize(cnpj);
        }

        /// <summary>
        /// Valida formato e dígitos verificadores do CNPJ numérico ou alfanumérico.
        /// </summary>
        protected bool IsValidCnpj(string cnpj)
        {
            return CnpjValidator.IsValid(cnpj);
        }

        /// <summary>
        /// Formata um CNPJ com pontuação, preservando os caracteres retornados pelo provedor.
        /// </summary>
        protected string FormatCnpj(string cnpj)
        {
            return CnpjValidator.Format(cnpj);
        }
    }
}
