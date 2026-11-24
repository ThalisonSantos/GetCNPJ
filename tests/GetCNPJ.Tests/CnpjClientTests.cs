using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GetCNPJ.Tests
{
    public class CnpjClientTests
    {
        // Dados inteiramente sintéticos; os DVs não indicam existência cadastral.
        private const string SyntheticCnpj = "12345678000195";

        [Fact]
        public async Task GetAsync_WithSuccessfulHttpResponse_ReturnsMappedData()
        {
            using var handler = new StubHttpMessageHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://brasilapi.com.br/api/cnpj/v1/" + SyntheticCnpj,
                    request.RequestUri!.AbsoluteUri);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"cnpj\":\"" + SyntheticCnpj + "\",\"razao_social\":\"EMPRESA FICTICIA DE TESTE\"}",
                        Encoding.UTF8, "application/json")
                };
            });
            using var httpClient = new HttpClient(handler);
            using var client = CreateClient(httpClient);

            var result = await client.GetAsync(SyntheticCnpj);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("12.345.678/0001-95", result.Data.Cnpj);
            Assert.Equal("EMPRESA FICTICIA DE TESTE", result.Data.RazaoSocial);
            Assert.Equal("BrasilAPI", result.Data.Provedor);
            Assert.Empty(result.Errors);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task GetAsync_WithHttpFailure_ReturnsProviderError()
        {
            using var handler = new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("indisponibilidade simulada")
                });
            using var httpClient = new HttpClient(handler);
            using var client = CreateClient(httpClient);

            var result = await client.GetAsync(SyntheticCnpj);

            Assert.False(result.Success);
            Assert.Null(result.Data);
            var error = Assert.Single(result.Errors);
            Assert.Equal("BrasilAPI", error.ProviderName);
            Assert.Contains("ServiceUnavailable", error.ErrorMessage);
            Assert.NotNull(error.Exception);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task GetFromProviderAsync_WithUnknownProvider_DoesNotSendHttpRequest()
        {
            using var handler = new StubHttpMessageHandler(_ =>
                throw new Xunit.Sdk.XunitException("Nenhuma requisição HTTP era esperada."));
            using var httpClient = new HttpClient(handler);
            using var client = CreateClient(httpClient);

            var result = await client.GetFromProviderAsync(SyntheticCnpj, "Inexistente");

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("Inexistente", result.ErrorMessage);
            Assert.Empty(result.Errors);
            Assert.Equal(0, handler.RequestCount);
        }

        private static CnpjClient CreateClient(HttpClient httpClient)
        {
            return new CnpjClient(httpClient, new CnpjClientOptions
            {
                EnableCNPJWS = false,
                EnableReceitaWS = false,
                EnableCNPJA = false,
                EnableBrasilAPI = true
            });
        }
    }
}
