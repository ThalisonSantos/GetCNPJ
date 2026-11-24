using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GetCNPJ.Exceptions;
using Xunit;

namespace GetCNPJ.Tests
{
    // Todos os dados são sintéticos; estes testes não consultam nem certificam APIs externas.
    public class ProviderContractTests
    {
        private const string Cnpj = "12345678000195";

        [Theory]
        [InlineData("BrasilAPI", "{}")]
        [InlineData("CNPJA", "{}")]
        [InlineData("CNPJWS", "{}")]
        [InlineData("ReceitaWS", "{}")]
        [InlineData("BrasilAPI", "null")]
        [InlineData("CNPJA", "null")]
        [InlineData("CNPJWS", "null")]
        [InlineData("ReceitaWS", "null")]
        public async Task EmptyHttpSuccess_IsNotSuccessfulQuery(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);
            var result = await client.GetFromProviderAsync(Cnpj, name);
            Assert.False(result.Success);
            Assert.Equal(1, handler.Requests);
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task InvalidDates_AreUnknownRegardlessOfCulture(string name)
        {
            var json = name == "CNPJWS"
                ? """{"estabelecimento":{"cnpj":"12345678000195","data_inicio_atividade":"02/03/2020"}}"""
                : HttpResourceDisposalTests.SuccessJson(name).Insert(1,
                    name == "ReceitaWS" ? """ "abertura":"02-03-2020", """ : """ "data_inicio_atividade":"02/03/2020", """);
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
                using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
                using var handler = new TrackedHandler(_ => response);
                using var http = new HttpClient(handler);
                var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
                Assert.NotNull(data);
                Assert.Null(data.DataAbertura);
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
        }

        [Theory]
        [InlineData("mismatch")]
        [InlineData("json-error")]
        public async Task InvalidFirstProviderResponse_AllowsFallbackAndPreservesOrder(string failure)
        {
            using var first = new TrackedHttpResponse(HttpStatusCode.OK, failure == "mismatch"
                ? """{"estabelecimento":{"cnpj":"12ABC34501DE35"}}"""
                : """{"status":404,"titulo":"Não encontrado","detalhes":"Falha sintética"}""");
            using var second = new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson("ReceitaWS"));
            using var handler = new TrackedHandler(request =>
            {
                if (request.RequestUri!.Host == "publica.cnpj.ws") return first;
                Assert.Equal("receitaws.com.br", request.RequestUri.Host);
                HttpResourceDisposalTests.AssertDisposed(first);
                return second;
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);
            var result = await client.GetAsync(Cnpj);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("ReceitaWS", result.Data.Provedor);
            Assert.Equal(2, handler.Requests);
            HttpResourceDisposalTests.AssertDisposed(second);
        }

        public static IEnumerable<object[]> CompleteResponses()
        {
            yield return new object[] { "BrasilAPI", """
                {"cnpj":"12345678000195","razao_social":"EMPRESA FICTICIA","nome_fantasia":"TESTE",
                 "situacao_cadastral":2,"descricao_situacao_cadastral":"ATIVA","data_situacao_cadastral":"2020-02-03",
                 "descricao_identificador_matriz_filial":"MATRIZ","identificador_matriz_filial":1,
                 "porte":"MICRO EMPRESA","codigo_porte":1,"natureza_juridica":"SOCIEDADE LIMITADA",
                 "data_inicio_atividade":"2020-01-02","capital_social":1234.56,"email":"teste@example.invalid",
                 "descricao_tipo_de_logradouro":"RUA","logradouro":"TESTE","numero":"0","complemento":null,
                 "bairro":"CENTRO","municipio":"MUNICIPIO TESTE","uf":"SP","cep":"00123000",
                 "codigo_municipio":9999,"codigo_municipio_ibge":3550308,"ddd_telefone_1":"1100000000",
                 "cnae_fiscal":111301,"cnae_fiscal_descricao":"ATIVIDADE FICTICIA",
                 "cnaes_secundarios":[null,{"codigo":111302,"descricao":"SECUNDARIA"}],
                 "qualificacao_do_responsavel":49,"qsa":[null,{"nome_socio":"SOCIO FICTICIO","qualificacao_socio":"ADMINISTRADOR","codigo_qualificacao_representante_legal":0}],
                 "opcao_pelo_simples":true,"opcao_pelo_mei":false,"data_opcao_pelo_simples":"2020-01-02"}
                """ };
            yield return new object[] { "CNPJA", """
                {"taxId":"12345678000195","updated":"2024-01-02T12:00:00Z","alias":"TESTE","founded":"2020-01-02",
                 "head":true,"status":{"id":2,"text":"ATIVA"},"statusDate":"2020-02-03",
                 "company":{"id":"12345678","name":"EMPRESA FICTICIA","equity":1234.56,"size":{"id":1,"text":"MICRO EMPRESA"},
                   "nature":{"id":2062,"text":"SOCIEDADE LIMITADA"},"members":[null,{"person":{"name":"SOCIO FICTICIO"},"role":{"id":49,"text":"ADMINISTRADOR"}}],
                   "simples":{"optant":true,"since":"2020-01-02"},"simei":{"optant":false,"since":null}},
                 "address":{"municipality":3550308,"street":"RUA TESTE","number":"0","district":"CENTRO",
                   "city":"MUNICIPIO TESTE","state":"SP","details":null,"zip":"00123000"},
                 "phones":[null,{"area":"11","number":"00000000"}],"emails":[null,{"address":null},{"address":"teste@example.invalid"}],
                 "mainActivity":{"id":111301,"text":"ATIVIDADE FICTICIA"},
                 "sideActivities":[null,{"id":111302,"text":"SECUNDARIA"}]}
                """ };
            yield return new object[] { "CNPJWS", """
                {"razao_social":"EMPRESA FICTICIA","capital_social":"1234.56","atualizado_em":"2024-01-02T12:00:00Z",
                 "porte":{"id":"01","descricao":"MICRO EMPRESA"},"natureza_juridica":{"id":"2062","descricao":"SOCIEDADE LIMITADA"},
                 "socios":[null,{"nome":"SOCIO FICTICIO","qualificacao_socio":{"id":49,"descricao":"ADMINISTRADOR"},"qualificacao_representante":"SEM REPRESENTANTE"}],
                 "simples":{"simples":"Sim","mei":"Não","data_opcao_simples":"2020-01-02"},
                 "estabelecimento":{"cnpj":"12345678000195","tipo":"MATRIZ","nome_fantasia":"TESTE","situacao_cadastral":"ATIVA",
                   "data_inicio_atividade":"2020-01-02","data_situacao_cadastral":"2020-02-03","motivo_situacao_cadastral":"SEM MOTIVO",
                   "tipo_logradouro":"RUA","logradouro":"TESTE","numero":"0","complemento":null,"bairro":"CENTRO","cep":"00123000",
                   "cidade":{"nome":"MUNICIPIO TESTE","ibge_id":3550308,"siafi_id":"9999"},"estado":{"sigla":"SP"},
                   "ddd1":"11","telefone1":"00000000","email":"teste@example.invalid",
                   "atividade_principal":{"subclasse":"0111301","descricao":"ATIVIDADE FICTICIA"},
                   "atividades_secundarias":[null,{"subclasse":"0111302","descricao":"SECUNDARIA"}],
                   "inscricoes_estaduais":[null,{"inscricao_estadual":""},{"inscricao_estadual":"000000000000","ativo":true,"estado":{"sigla":"SP"},"atualizado_em":"2024-01-02T12:00:00Z"}]}}
                """ };
            yield return new object[] { "ReceitaWS", """
                {"status":"OK","cnpj":"12345678000195","nome":"EMPRESA FICTICIA","fantasia":"TESTE","tipo":"MATRIZ",
                 "situacao":"ATIVA","data_situacao":"03/02/2020","abertura":"02/01/2020","porte":"MICRO EMPRESA",
                 "natureza_juridica":"2062 - SOCIEDADE LIMITADA","capital_social":"1234.56",
                 "logradouro":"RUA TESTE","numero":"0","complemento":null,"bairro":"CENTRO","municipio":"MUNICIPIO TESTE","uf":"SP","cep":"00123-000",
                 "email":"teste@example.invalid","telefone":"(11) 0000-0000 / (11) 00000-0000",
                 "atividade_principal":[null,{"code":"01.11-3-01","text":"ATIVIDADE FICTICIA"}],
                 "atividades_secundarias":[null,{"code":"01.11-3-02","text":"SECUNDARIA"}],
                 "qsa":[null,{"nome":"SOCIO FICTICIO","qual":"ADMINISTRADOR"}],
                 "simples":{"optante":true,"data_opcao":"02/01/2020","data_exclusao":null},"simei":{"optante":false},
                 "ultima_atualizacao":"2024-01-02T12:00:00Z"}
                """ };
        }

        [Theory]
        [MemberData(nameof(CompleteResponses))]
        public async Task CompleteDocumentedResponse_MapsWithoutLosingValues(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.Equal("EMPRESA FICTICIA", data.RazaoSocial);
            Assert.Equal("TESTE", data.NomeFantasia);
            Assert.Equal("MATRIZ", data.Tipo);
            Assert.Equal("ATIVA", data.Situacao);
            Assert.Equal("MICRO EMPRESA", data.Porte);
            Assert.Contains("SOCIEDADE LIMITADA", data.NaturezaJuridica);
            Assert.Equal(1234.56m, data.CapitalSocial);
            Assert.Equal(new DateTime(2020, 1, 2), data.DataAbertura);
            Assert.Equal(new DateTime(2020, 2, 3), data.DataSituacao);
            Assert.NotNull(data.Endereco);
            Assert.Equal("RUA TESTE", data.Endereco.Logradouro);
            Assert.Equal("00123-000", data.Endereco.Cep);
            Assert.Equal(name == "ReceitaWS" ? null : "3550308", data.Endereco.CodigoIbge);
            Assert.Equal("teste@example.invalid", data.Email);
            Assert.Contains("(11) 0000-0000", data.Telefones);
            Assert.NotNull(data.AtividadePrincipal);
            Assert.Equal("01.11-3-01", data.AtividadePrincipal.Codigo);
            Assert.Equal("01.11-3-02", Assert.Single(data.AtividadesSecundarias).Codigo);
            Assert.Equal("SOCIO FICTICIO", Assert.Single(data.QuadroSocietario).Nome);
            Assert.NotNull(data.Simples);
            Assert.True(data.Simples.Optante);
            Assert.True(data.Simples.OptanteInformado);
            Assert.False(data.Simples.OptanteSimei);
            Assert.False(data.Simples.OptanteSimeiInformado);
            Assert.Equal(new DateTime(2020, 1, 2), data.Simples.DataOpcao);
            if (name == "CNPJWS")
            {
                var ie = Assert.Single(data.InscricoesEstaduais);
                Assert.Equal("000000000000", ie.Inscricao);
                Assert.True(ie.AtivoInformado);
                Assert.True(ie.Ativo);
                Assert.Equal("SP", ie.Estado);
            }
            else Assert.Empty(data.InscricoesEstaduais);
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        public static IEnumerable<object[]> PartialResponses()
        {
            foreach (var name in new[] { "BrasilAPI", "CNPJA", "CNPJWS", "ReceitaWS" })
            {
                yield return new object[] { name, HttpResourceDisposalTests.SuccessJson(name) };
                var body = name == "CNPJWS"
                    ? """{"estabelecimento":{"cnpj":"12345678000195","cidade":null,"estado":null,"atividade_principal":null,"atividades_secundarias":null,"inscricoes_estaduais":null},"simples":{"simples":null,"mei":null},"socios":null,"capital_social":null}"""
                    : name == "CNPJA"
                    ? """{"taxId":"12345678000195","head":null,"company":{"name":null,"equity":null,"simples":{"optant":null},"simei":{"optant":null},"members":null},"phones":null,"emails":null,"address":null,"mainActivity":null,"sideActivities":null,"status":null}"""
                    : name == "BrasilAPI"
                    ? """{"cnpj":"12345678000195","situacao_cadastral":null,"porte":null,"capital_social":null,"codigo_municipio_ibge":null,"qsa":null,"cnaes_secundarios":null,"opcao_pelo_simples":null,"opcao_pelo_mei":null}"""
                    : """{"cnpj":"12345678000195","atividade_principal":null,"atividades_secundarias":null,"qsa":null,"simples":{"optante":null},"simei":{"optante":null},"capital_social":null}""";
                yield return new object[] { name, body };
            }
        }

        [Theory]
        [MemberData(nameof(PartialResponses))]
        public async Task PartialAndNullResponses_PreserveUnknownAndEmptyLists(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.Null(data.CapitalSocial);
            Assert.Null(data.DataAbertura);
            Assert.Null(data.Situacao);
            Assert.NotNull(data.Endereco);
            Assert.Null(data.Endereco.CodigoIbge);
            Assert.Null(data.Tipo);
            Assert.Empty(data.Telefones);
            Assert.Empty(data.InscricoesEstaduais);
            Assert.Empty(data.AtividadesSecundarias);
            Assert.Empty(data.QuadroSocietario);
            if (data.Simples != null)
            {
                Assert.Null(data.Simples.OptanteInformado);
                Assert.Null(data.Simples.OptanteSimeiInformado);
                Assert.False(data.Simples.Optante);
                Assert.False(data.Simples.OptanteSimei);
            }
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        public static IEnumerable<object[]> RejectedResponses()
        {
            foreach (var name in new[] { "BrasilAPI", "CNPJA", "CNPJWS", "ReceitaWS" })
            {
                var valid = HttpResourceDisposalTests.SuccessJson(name);
                foreach (var identifier in new[] { "", "12345678000194", "1234567800019!", "12ABC34501DE35" })
                    yield return new object[] { name, valid.Replace(Cnpj, identifier) };
                foreach (var envelope in new[] { """ "status":"ERROR","message":"falha simulada" """, """ "status":429,"titulo":"limite" """, """ "error":"falha simulada" """, """ "errors":[{"message":"falha simulada"}] """ })
                    yield return new object[] { name, valid.Insert(1, envelope + ",") };
                yield return new object[] { name, "{" };
                yield return new object[] { name, valid.Replace("\"" + Cnpj + "\"", "true") };
            }
        }

        [Theory]
        [MemberData(nameof(RejectedResponses))]
        public async Task InvalidIdentityOrJsonError_IsFailureAndDisposesResponse(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            await Assert.ThrowsAsync<ProviderException>(() => HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj));
            HttpResourceDisposalTests.AssertDisposed(response);
            Assert.Equal(1, handler.Requests);
        }

        [Theory]
        [InlineData("ReceitaWS", """{"cnpj":"12345678000195","simei":{"optante":true}}""")]
        [InlineData("CNPJA", """{"taxId":"12345678000195","company":{"simei":{"optant":true}}}""")]
        [InlineData("BrasilAPI", """{"cnpj":"12345678000195","opcao_pelo_mei":true}""")]
        [InlineData("CNPJWS", """{"estabelecimento":{"cnpj":"12345678000195"},"simples":{"mei":"Sim"}}""")]
        public async Task MeiWithoutSimples_PreservesIndependentInformation(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.NotNull(data.Simples);
            Assert.Null(data.Simples.OptanteInformado);
            Assert.False(data.Simples.Optante);
            Assert.True(data.Simples.OptanteSimeiInformado);
            Assert.True(data.Simples.OptanteSimei);
        }

        [Theory]
        [InlineData("ReceitaWS")]
        [InlineData("CNPJWS")]
        public async Task MalformedCapital_IsUnknownRatherThanInflated(string name)
        {
            var json = HttpResourceDisposalTests.SuccessJson(name).Insert(1, """ "capital_social":"1234,56", """);
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.Null(data.CapitalSocial);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StateRegistration_DistinguishesMissingAndNegativeStatus(bool? active)
        {
            var value = active.HasValue ? (active.Value ? "true" : "false") : "null";
            var json = """{"estabelecimento":{"cnpj":"12345678000195","inscricoes_estaduais":[{"inscricao_estadual":"000000000000","ativo":VALUE,"estado":null}]}}""".Replace("VALUE", value);
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider("CNPJWS", http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            var ie = Assert.Single(data.InscricoesEstaduais);
            Assert.Equal(active, ie.AtivoInformado);
            Assert.Equal(active ?? false, ie.Ativo);
        }

        [Fact]
        public async Task BrasilApi_AcceptsDocumentedNumericCodesAndTextualSize()
        {
            var json = "{\"cnpj\":\"" + Cnpj + "\",\"situacao_cadastral\":2,\"porte\":\"MICRO EMPRESA\","
                + "\"qualificacao_do_responsavel\":49,\"qsa\":[{\"codigo_qualificacao_representante_legal\":0}],"
                + "\"descricao_identificador_matriz_filial\":\"MATRIZ\",\"descricao_tipo_de_logradouro\":\"RUA\",\"logradouro\":\"TESTE\"}";
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider("BrasilAPI", http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.Equal("MICRO EMPRESA", data.Porte);
            Assert.Equal("MATRIZ", data.Tipo);
            Assert.Equal("ATIVA", data.Situacao);
            Assert.NotNull(data.Endereco);
            Assert.Equal("RUA TESTE", data.Endereco.Logradouro);
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task ReturnedIdentifierMustMatchRequest(string name)
        {
            // Outro CNPJ sintético válido: divergência, não erro de DV.
            var json = HttpResourceDisposalTests.SuccessJson(name).Replace(Cnpj, "12ABC34501DE35");
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            await Assert.ThrowsAsync<ProviderException>(() => HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj));
            HttpResourceDisposalTests.AssertDisposed(response);
            Assert.Equal(1, handler.Requests);
        }
    }
}
