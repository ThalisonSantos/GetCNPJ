using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using GetCNPJ.Exceptions;
using GetCNPJ.Interfaces;
using GetCNPJ.Models;
using GetCNPJ.Services;
using Xunit;

namespace GetCNPJ.Tests
{
    public class NullabilityTests
    {
        private const string Cnpj = "12345678000195";

        public static IEnumerable<object[]> OptionalDtos()
        {
            var namespaces = new[] { "GetCNPJ.Providers.BrasilAPI", "GetCNPJ.Providers.CNPJA", "GetCNPJ.Providers.CNPJWS", "GetCNPJ.Providers.ReceitaWS" };
            foreach (var type in typeof(CnpjClient).Assembly.GetTypes().Where(t => t.IsClass && !t.IsPublic && !t.IsNested && t.Namespace is string ns && namespaces.Contains(ns)).OrderBy(t => t.FullName))
            foreach (var explicitNull in new[] { false, true })
                yield return new object[] { type, explicitNull };
        }

        [Theory]
        [MemberData(nameof(OptionalDtos))]
        public void EveryDto_AcceptsMissingAndNullPropertiesWithoutInventingValues(Type type, bool explicitNull)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var json = explicitNull ? JsonSerializer.Serialize(properties.ToDictionary(p => p.Name, _ => (object?)null)) : "{}";
            var dto = JsonSerializer.Deserialize(json, type);
            Assert.NotNull(dto);
            foreach (var property in properties)
            {
                var value = property.GetValue(dto);
                if (value is JsonElement element)
                    Assert.Equal(explicitNull ? JsonValueKind.Null : JsonValueKind.Undefined, element.ValueKind);
                else
                    Assert.Null(value);
            }
        }

        [Fact]
        public void PublicModels_PreserveMissingInformationAndInitializedCollections()
        {
            var models = new object[] { new CnpjData(), new CnpjResult(), new ProviderError(), new Endereco(), new AtividadeEconomica(), new Socio(), new InscricaoEstadual() };
            foreach (var model in models)
            foreach (var property in model.GetType().GetProperties().Where(p => p.CanWrite && (p.PropertyType == typeof(string) || p.PropertyType == typeof(Exception) || p.PropertyType == typeof(Endereco) || p.PropertyType == typeof(AtividadeEconomica) || p.PropertyType == typeof(SimplesNacional) || p.PropertyType == typeof(CnpjData))))
                Assert.Null(property.GetValue(model));

            var data = Assert.IsType<CnpjData>(models[0]);
            Assert.Empty(data.AtividadesSecundarias);
            Assert.Empty(data.QuadroSocietario);
            Assert.Empty(data.Telefones);
            Assert.Empty(data.InscricoesEstaduais);
            Assert.Null(data.CapitalSocial);
            Assert.Null(data.DataAbertura);
            var simples = new SimplesNacional();
            Assert.Null(simples.OptanteInformado);
            Assert.Null(simples.OptanteSimeiInformado);
            Assert.False(simples.Optante);
            Assert.False(simples.OptanteSimei);
            Assert.Null(new InscricaoEstadual().AtivoInformado);
            Assert.Null(CnpjResult.CreateError("falha", null).Data);
            Assert.Null(CnpjResult.CreateSuccess(data).ErrorMessage);
        }

        [Fact]
        public void SuccessFactory_RejectsMissingRequiredData()
        {
            // Reflection permite testar entrada inválida sem contradizer a anotação do parâmetro.
            var factory = typeof(CnpjResult).GetMethod(nameof(CnpjResult.CreateSuccess));
            Assert.NotNull(factory);
            var error = Assert.Throws<TargetInvocationException>(() => factory.Invoke(null, new object?[] { null }));
            var inner = Assert.IsType<ArgumentNullException>(error.InnerException);
            Assert.Equal("data", inner.ParamName);
        }

        [Fact]
        public void FailedProviders_DoesNotDereferenceNullJsonEntriesOrInventNames()
        {
            var result = JsonSerializer.Deserialize<CnpjResult>("""{"Errors":[null,{},{"ProviderName":null},{"ProviderName":"TESTE"}]}""");
            Assert.NotNull(result);
            Assert.Equal(new[] { "TESTE" }, result.FailedProviders);
        }

        [Fact]
        public void NullProviders_AreRejectedAsConfigurationErrors()
        {
            // Cast permite construir uma coleção inválida para testar o contrato, sem null-forgiving.
            var invalid = new ICnpjProvider?[] { null }.Cast<ICnpjProvider>();
            var error = Assert.Throws<ArgumentException>(() => new CnpjService(invalid));
            Assert.Equal("providers", error.ParamName);
        }

        [Fact]
        public async Task ProviderCollection_IsCapturedOnceBeforeExternalMutation()
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson("BrasilAPI"));
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var providers = new List<ICnpjProvider> { HttpResourceDisposalTests.CreateProvider("BrasilAPI", http) };
            var service = new CnpjService(providers);
            providers.Clear();
            var result = await service.GetCnpjAsync(Cnpj);
            Assert.True(result.Success);
            Assert.Equal(1, handler.Requests);
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task IdentityOnlyResponse_DoesNotFabricateOptionalData(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson(name));
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.NotNull(data.Endereco);
            Assert.Null(data.Endereco.Logradouro);
            Assert.Null(data.Endereco.CodigoIbge);
            Assert.Null(data.RazaoSocial);
            Assert.Null(data.Tipo);
            Assert.Null(data.NaturezaJuridica);
            Assert.Null(data.CapitalSocial);
            Assert.Null(data.AtividadePrincipal);
            Assert.Empty(data.Telefones);
            if (data.Simples != null)
            {
                Assert.Null(data.Simples.OptanteInformado);
                Assert.Null(data.Simples.OptanteSimeiInformado);
            }
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        public static IEnumerable<object[]> IncompleteResponses()
        {
            foreach (var explicitNull in new[] { false, true })
            {
                var activity = explicitNull ? """{"id":null,"text":null}""" : "{}";
                var nature = explicitNull ? """{"id":null,"text":null}""" : "{}";
                var role = explicitNull ? """{"id":null,"text":null}""" : "{}";
                yield return new object[] { "CNPJA", """{"taxId":"12345678000195","mainActivity":ACTIVITY,"sideActivities":[null,ACTIVITY],"company":{"nature":NATURE,"members":[null,{"person":null,"role":ROLE}]},"address":{},"phones":[null,{}],"emails":[null,{}]}""".Replace("ACTIVITY", activity).Replace("NATURE", nature).Replace("ROLE", role) };
                yield return new object[] { "CNPJWS", explicitNull
                    ? """{"natureza_juridica":{"id":null,"descricao":null},"socios":[null,{"qualificacao_socio":{"id":null,"descricao":null}}],"estabelecimento":{"cnpj":"12345678000195","cidade":{"id":null,"ibge_id":null},"estado":null,"atividade_principal":{"subclasse":null},"atividades_secundarias":[null,{}],"inscricoes_estaduais":[null,{}]}}"""
                    : """{"natureza_juridica":{},"socios":[null,{}],"estabelecimento":{"cnpj":"12345678000195","cidade":{},"estado":{},"atividade_principal":{},"atividades_secundarias":[null,{}],"inscricoes_estaduais":[null,{}]}}""" };
                yield return new object[] { "BrasilAPI", explicitNull
                    ? """{"cnpj":"12345678000195","cnaes_secundarios":[null,{"codigo":null,"descricao":null}],"qsa":[null,{"nome_socio":null}],"codigo_municipio_ibge":null,"porte":null}"""
                    : """{"cnpj":"12345678000195","cnaes_secundarios":[null,{}],"qsa":[null,{}]}""" };
                yield return new object[] { "ReceitaWS", explicitNull
                    ? """{"cnpj":"12345678000195","atividade_principal":[null,{"code":null,"text":null}],"atividades_secundarias":[null,{"code":null,"text":null}],"qsa":[null,{"nome":null,"qual":null}],"simples":null}"""
                    : """{"cnpj":"12345678000195","atividade_principal":[null,{}],"atividades_secundarias":[null,{}],"qsa":[null,{}],"simples":{}}""" };
            }
        }

        [Theory]
        [MemberData(nameof(IncompleteResponses))]
        public async Task NestedMissingAndNullFields_MapAsUnknownWithoutExceptions(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var data = await HttpResourceDisposalTests.CreateProvider(name, http).GetCnpjDataAsync(Cnpj);
            Assert.NotNull(data);
            Assert.NotNull(data.Endereco);
            Assert.Null(data.Endereco.Logradouro);
            Assert.Null(data.Endereco.CodigoIbge);
            Assert.Null(data.NaturezaJuridica);
            Assert.Empty(data.Telefones);
            Assert.Empty(data.InscricoesEstaduais);
            Assert.Null(Assert.Single(data.QuadroSocietario).Nome);
            Assert.Null(Assert.Single(data.QuadroSocietario).Qualificacao);
            if (name == "BrasilAPI")
            {
                Assert.Null(data.AtividadePrincipal);
                Assert.Empty(data.AtividadesSecundarias);
            }
            else
            {
                Assert.NotNull(data.AtividadePrincipal);
                Assert.Null(data.AtividadePrincipal.Codigo);
                Assert.Null(Assert.Single(data.AtividadesSecundarias).Codigo);
            }
            HttpResourceDisposalTests.AssertDisposed(response);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("true")]
        public async Task BrasilSize_RejectsInvalidTypesRatherThanIgnoringThem(string value)
        {
            var json = """{"cnpj":"12345678000195","porte":VALUE}""".Replace("VALUE", value);
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            var error = await Assert.ThrowsAsync<ProviderException>(() => HttpResourceDisposalTests.CreateProvider("BrasilAPI", http).GetCnpjDataAsync(Cnpj));
            Assert.IsAssignableFrom<JsonException>(error.InnerException);
            HttpResourceDisposalTests.AssertDisposed(response);
        }
    }
}
