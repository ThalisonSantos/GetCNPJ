using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Interfaces;
using GetCNPJ.Models;
using GetCNPJ.Providers.Base;

namespace GetCNPJ.Providers.CNPJA
{
    /// <summary>
    /// Provider para CNPJA - https://open.cnpja.com
    /// </summary>
    public class CNPJAProvider : CnpjProviderBase
    {
        public override string ProviderName => "CNPJA";
        public override int Priority => 4;
        protected override string BaseUrl => "https://open.cnpja.com/office/";

        public CNPJAProvider(HttpClient httpClient, IRateLimiter rateLimiter)
            : base(httpClient, rateLimiter)
        {
        }

        protected override async Task<CnpjData?> FetchDataAsync(string cnpj, CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}{cnpj}";
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Erro na requisição: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = ProviderJson.Deserialize<CNPJAResponse>(content);

            if (data == null)
            {
                return null;
            }

            return MapToCnpjData(data);
        }

        private CnpjData MapToCnpjData(CNPJAResponse response)
        {
            var cnpjData = new CnpjData
            {
                Cnpj = FormatCnpj(response.taxId),
                RazaoSocial = response.company?.name,
                NomeFantasia = response.alias,
                DataAbertura = response.founded,
                Situacao = response.status?.text,
                DataSituacao = response.statusDate,
                Tipo = response.head is bool head ? (head ? "MATRIZ" : "FILIAL") : null,
                Porte = response.company?.size?.text,
                NaturezaJuridica = ProviderFormatting.Join(response.company?.nature?.id?.ToString(CultureInfo.InvariantCulture), response.company?.nature?.text, " - "),
                CapitalSocial = response.company?.equity,
                UltimaAtualizacao = response.updated,
                Endereco = new Endereco
                {
                    Cep = FormatCep(response.address?.zip),
                    Logradouro = response.address?.street,
                    Numero = response.address?.number,
                    Complemento = response.address?.details,
                    Bairro = response.address?.district,
                    Municipio = response.address?.city,
                    Uf = response.address?.state,
                    CodigoIbge = response.address?.municipality?.ToString(CultureInfo.InvariantCulture)
                }
            };

            // Email
            if (response.emails != null && response.emails.Any())
            {
                cnpjData.Email = response.emails.FirstOrDefault(e => e != null && !string.IsNullOrWhiteSpace(e.address))?.address;
            }

            // Telefones
            if (response.phones != null)
            {
                cnpjData.Telefones = response.phones
                    .OfType<PhoneCNPJA>()
                    .Where(p => !string.IsNullOrWhiteSpace(p.number))
                    .Select(p => FormatPhone(p.area, p.number))
                    .OfType<string>()
                    .ToList();
            }

            // Atividade principal
            if (response.mainActivity != null)
            {
                cnpjData.AtividadePrincipal = new AtividadeEconomica
                {
                    Codigo = FormatCnaeId(response.mainActivity.id),
                    Descricao = response.mainActivity.text
                };
            }

            // Atividades secundárias
            if (response.sideActivities != null)
            {
                cnpjData.AtividadesSecundarias = response.sideActivities
                    .OfType<ActivityCNPJA>()
                    .Select(a => new AtividadeEconomica
                    {
                        Codigo = FormatCnaeId(a.id),
                        Descricao = a.text
                    })
                    .ToList();
            }

            // Quadro societário
            if (response.company?.members != null)
            {
                cnpjData.QuadroSocietario = response.company.members
                    .OfType<MemberCNPJA>()
                    .Select(m => new Socio
                    {
                        Nome = m.person?.name,
                        Qualificacao = ProviderFormatting.Join(m.role?.id?.ToString(CultureInfo.InvariantCulture), m.role?.text, "-")
                    })
                    .ToList();
            }

            // Simples Nacional
            cnpjData.Simples = new SimplesNacional
            {
                Optante = response.company?.simples?.optant ?? false,
                OptanteInformado = response.company?.simples?.optant,
                OptanteSimeiInformado = response.company?.simei?.optant,
                DataOpcao = response.company?.simples?.since,
                OptanteSimei = response.company?.simei?.optant ?? false
            };

            return cnpjData;
        }

        private string? FormatCep(string? cep)
        {
            if (cep == null || string.IsNullOrWhiteSpace(cep) || cep.Length != 8)
                return cep;

            return $"{cep.Substring(0, 5)}-{cep.Substring(5, 3)}";
        }

        private string? FormatPhone(string? area, string? number)
        {
            if (number == null) return null;
            if (string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(number))
                return $"{area}{number}";

            if (number.Length == 8)
                return $"({area}) {number.Substring(0, 4)}-{number.Substring(4, 4)}";

            if (number.Length == 9)
                return $"({area}) {number.Substring(0, 5)}-{number.Substring(5, 4)}";

            return $"({area}) {number}";
        }

        private string? FormatCnaeId(int? cnaeId)
        {
            if (cnaeId is not int code) return null;
            var cnaeStr = code.ToString("D7", CultureInfo.InvariantCulture);
            return $"{cnaeStr.Substring(0, 2)}.{cnaeStr.Substring(2, 2)}-{cnaeStr.Substring(4, 1)}-{cnaeStr.Substring(5, 2)}";
        }

    }
}
