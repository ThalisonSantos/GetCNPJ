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

namespace GetCNPJ.Providers.BrasilAPI
{
    /// <summary>
    /// Provider para BrasilAPI - https://brasilapi.com.br
    /// </summary>
    public class BrasilAPIProvider : CnpjProviderBase
    {
        public override string ProviderName => "BrasilAPI";
        public override int Priority => 3;
        protected override string BaseUrl => "https://brasilapi.com.br/api/cnpj/v1/";

        public BrasilAPIProvider(HttpClient httpClient, IRateLimiter rateLimiter)
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
            var data = ProviderJson.Deserialize<BrasilAPIResponse>(content);

            if (data == null)
            {
                return null;
            }

            return MapToCnpjData(data);
        }

        private CnpjData MapToCnpjData(BrasilAPIResponse response)
        {
            var cnpjData = new CnpjData
            {
                Cnpj = FormatCnpj(response.cnpj),
                RazaoSocial = response.razao_social,
                NomeFantasia = response.nome_fantasia,
                DataAbertura = ParseDate(response.data_inicio_atividade),
                Situacao = response.descricao_situacao_cadastral ?? DescribeStatus(response.situacao_cadastral),
                DataSituacao = ParseDate(response.data_situacao_cadastral),
                Tipo = response.descricao_identificador_matriz_filial ?? response.descricao_matriz_filial,
                Porte = ReadSize(response.porte, response.descricao_porte),
                NaturezaJuridica = response.natureza_juridica,
                CapitalSocial = response.capital_social,
                Email = response.email,
                Endereco = new Endereco
                {
                    Cep = FormatCep(response.cep),
                    Logradouro = ProviderFormatting.Join(response.descricao_tipo_de_logradouro ?? response.descricao_tipo_logradouro, response.logradouro, " ")?.Trim(),
                    CodigoIbge = response.codigo_municipio_ibge?.ToString(CultureInfo.InvariantCulture),
                    Numero = response.numero,
                    Complemento = response.complemento,
                    Bairro = response.bairro,
                    Municipio = response.municipio,
                    Uf = response.uf
                }
            };

            // Telefones
            if (response.ddd_telefone_1 is string phone1 && !string.IsNullOrWhiteSpace(phone1))
                cnpjData.Telefones.Add(FormatPhone(phone1));

            if (response.ddd_telefone_2 is string phone2 && !string.IsNullOrWhiteSpace(phone2))
                cnpjData.Telefones.Add(FormatPhone(phone2));

            // Atividade principal
            if (response.cnae_fiscal is int mainCode)
            {
                cnpjData.AtividadePrincipal = new AtividadeEconomica
                {
                    Codigo = FormatCnae(mainCode),
                    Descricao = response.cnae_fiscal_descricao
                };
            }

            // Atividades secundárias
            if (response.cnaes_secundarios != null)
            {
                foreach (var activity in response.cnaes_secundarios.OfType<CnaeFiscalBrasil>())
                {
                    if (activity.codigo is int code)
                    {
                        cnpjData.AtividadesSecundarias.Add(new AtividadeEconomica
                        {
                            Codigo = FormatCnae(code),
                            Descricao = activity.descricao
                        });
                    }
                }
            }

            // Quadro societário
            if (response.qsa != null)
            {
                cnpjData.QuadroSocietario = response.qsa
                    .OfType<QsaBrasil>()
                    .Select(s => new Socio
                    {
                        Nome = s.nome_socio,
                        Qualificacao = s.qualificacao_socio
                    })
                    .ToList();
            }

            // Simples Nacional
            cnpjData.Simples = new SimplesNacional
            {
                Optante = response.opcao_pelo_simples ?? false,
                OptanteInformado = response.opcao_pelo_simples,
                OptanteSimeiInformado = response.opcao_pelo_mei,
                DataOpcao = ParseDate(response.data_opcao_pelo_simples),
                DataExclusao = ParseDate(response.data_exclusao_do_simples),
                OptanteSimei = response.opcao_pelo_mei ?? false
            };

            return cnpjData;
        }

        private DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            return null;
        }

        private static string? DescribeStatus(int? code)
        {
            switch (code)
            {
                case 1: return "NULA";
                case 2: return "ATIVA";
                case 3: return "SUSPENSA";
                case 4: return "INAPTA";
                case 8: return "BAIXADA";
                default: return null;
            }
        }

        private static string? ReadSize(JsonElement value, string? legacyDescription)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String: return value.GetString();
                case JsonValueKind.Number:
                    if (value.TryGetInt32(out _)) return legacyDescription;
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined: return legacyDescription;
            }
            throw new JsonException("O porte retornado deve ser textual ou um código inteiro legado.");
        }

        private string? FormatCep(string? cep)
        {
            if (cep == null || string.IsNullOrWhiteSpace(cep) || cep.Length != 8)
                return cep;

            return $"{cep.Substring(0, 5)}-{cep.Substring(5, 3)}";
        }

        private string FormatCnae(int cnae)
        {
            var cnaeStr = cnae.ToString("D7");
            return $"{cnaeStr.Substring(0, 2)}.{cnaeStr.Substring(2, 2)}-{cnaeStr.Substring(4, 1)}-{cnaeStr.Substring(5, 2)}";
        }

        private string FormatPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Remove caracteres não numéricos
            phone = new string(phone.Where(char.IsDigit).ToArray());

            if (phone.Length == 10)
                return $"({phone.Substring(0, 2)}) {phone.Substring(2, 4)}-{phone.Substring(6, 4)}";

            if (phone.Length == 11)
                return $"({phone.Substring(0, 2)}) {phone.Substring(2, 5)}-{phone.Substring(7, 4)}";

            return phone;
        }
    }
}
