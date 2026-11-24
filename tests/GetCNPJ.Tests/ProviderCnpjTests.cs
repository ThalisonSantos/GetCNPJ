using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GetCNPJ.Exceptions;
using GetCNPJ.Interfaces;
using GetCNPJ.Providers.BrasilAPI;
using GetCNPJ.Providers.CNPJA;
using GetCNPJ.Providers.CNPJWS;
using GetCNPJ.Providers.ReceitaWS;
using GetCNPJ.RateLimiter;
using Xunit;

namespace GetCNPJ.Tests
{
    // Exercita somente os adaptadores locais. HTTP simulado não comprova suporte das APIs.
    public class ProviderCnpjTests
    {
        private static readonly string[] ProviderNames = { "BrasilAPI", "CNPJA", "CNPJWS", "ReceitaWS" };

        public static IEnumerable<object[]> ValidRequests()
        {
            foreach (var provider in ProviderNames)
            foreach (var input in new[] { "12345678000195", "12.345.678/0001-95", "12ABC34501DE35", "12.abc.345/01de-35" })
            foreach (var formattedResponse in new[] { false, true })
            {
                var isNumeric = input == "12345678000195" || input == "12.345.678/0001-95";
                yield return new object[]
                {
                    provider, input,
                    isNumeric ? "12345678000195" : "12ABC34501DE35",
                    isNumeric ? "12.345.678/0001-95" : "12.ABC.345/01DE-35",
                    formattedResponse
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidRequests))]
        public async Task GetCnpjDataAsync_PreservesIdentifierInUrlAndMappedResponse(
            string providerName, string input, string normalized, string formatted, bool formattedResponse)
        {
            var returnedCnpj = formattedResponse ? formatted : normalized;
            var json = CreateJson(providerName, returnedCnpj);
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            using var handler = new StubHttpMessageHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal(GetEndpoint(providerName) + normalized, request.RequestUri!.AbsoluteUri);
                return response;
            });
            using var httpClient = new HttpClient(handler);
            var provider = CreateProvider(providerName, httpClient);

            var data = await provider.GetCnpjDataAsync(input);

            Assert.NotNull(data);
            // ReceitaWS historicamente preserva a máscara recebida; os demais aplicam FormatCnpj.
            Assert.Equal(providerName == "ReceitaWS" ? returnedCnpj : formatted, data.Cnpj);
            Assert.Equal("EMPRESA FICTICIA", data.RazaoSocial);
            Assert.Equal(providerName, data.Provedor);
            Assert.Equal(1, handler.RequestCount);
        }

        public static IEnumerable<object[]> InvalidRequests()
        {
            foreach (var provider in ProviderNames)
            foreach (var input in new[] { "12ABC34501DE34", "12345678000194", "A12345678000195", "12345678000195!", "12ABC34501DE3A" })
                yield return new object[] { provider, input };
        }

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task GetCnpjDataAsync_WithInvalidIdentifier_RejectsBeforeHttp(string providerName, string input)
        {
            using var handler = new StubHttpMessageHandler(_ =>
                throw new Xunit.Sdk.XunitException("Nenhuma requisição HTTP era esperada."));
            using var httpClient = new HttpClient(handler);
            var provider = CreateProvider(providerName, httpClient);

            await Assert.ThrowsAsync<InvalidCnpjException>(() => provider.GetCnpjDataAsync(input));

            Assert.Equal(0, handler.RequestCount);
        }

        private static ICnpjProvider CreateProvider(string name, HttpClient httpClient)
        {
            var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromMinutes(1));
            switch (name)
            {
                case "BrasilAPI": return new BrasilAPIProvider(httpClient, limiter);
                case "CNPJA": return new CNPJAProvider(httpClient, limiter);
                case "CNPJWS": return new CNPJWSProvider(httpClient, limiter);
                case "ReceitaWS": return new ReceitaWSProvider(httpClient, limiter);
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        private static string GetEndpoint(string name)
        {
            switch (name)
            {
                case "BrasilAPI": return "https://brasilapi.com.br/api/cnpj/v1/";
                case "CNPJA": return "https://open.cnpja.com/office/";
                case "CNPJWS": return "https://publica.cnpj.ws/cnpj/";
                case "ReceitaWS": return "https://receitaws.com.br/v1/cnpj/";
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        private static string CreateJson(string name, string cnpj)
        {
            switch (name)
            {
                case "BrasilAPI": return "{\"cnpj\":\"" + cnpj + "\",\"razao_social\":\"EMPRESA FICTICIA\"}";
                case "CNPJA": return "{\"taxId\":\"" + cnpj + "\",\"company\":{\"name\":\"EMPRESA FICTICIA\"}}";
                case "CNPJWS": return "{\"razao_social\":\"EMPRESA FICTICIA\",\"estabelecimento\":{\"cnpj\":\"" + cnpj + "\"}}";
                case "ReceitaWS": return "{\"status\":\"OK\",\"nome\":\"EMPRESA FICTICIA\",\"cnpj\":\"" + cnpj + "\"}";
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }
    }
}
