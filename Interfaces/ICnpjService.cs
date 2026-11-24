using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Models;

namespace GetCNPJ.Interfaces
{
    /// <summary>
    /// Interface do serviço principal de consulta de CNPJ
    /// </summary>
    public interface ICnpjService
    {
        /// <summary>
        /// Consulta dados de um CNPJ utilizando os provedores disponíveis
        /// Timeouts/falhas permitem a próxima tentativa; cancelamento do consumidor encerra a operação.
        /// </summary>
        /// <param name="cnpj">CNPJ a ser consultado (pode conter formatação)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da consulta</returns>
        Task<CnpjResult> GetCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta dados de um CNPJ utilizando um provedor específico
        /// Não faz fallback. Timeout retorna resultado de falha; cancelamento do consumidor é propagado.
        /// </summary>
        /// <param name="cnpj">CNPJ a ser consultado</param>
        /// <param name="providerName">Nome do provedor</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da consulta</returns>
        Task<CnpjResult> GetCnpjFromProviderAsync(string cnpj, string providerName, CancellationToken cancellationToken = default);
    }
}
