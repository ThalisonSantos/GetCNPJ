using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public class HttpResourceDisposalTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SynchronousQueries_DisposeBeforeReturning(bool directed)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, SuccessJson("BrasilAPI"));
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http, new CnpjClientOptions
            {
                EnableBrasilAPI = true, EnableCNPJA = false,
                EnableCNPJWS = false, EnableReceitaWS = false
            });

            var result = directed ? client.GetFromProvider("12345678000195", "BrasilAPI") : client.Get("12345678000195");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            AssertDisposed(response);
            Assert.Equal(1, handler.Requests);
        }

        public static IEnumerable<object[]> HttpErrors()
        {
            foreach (var name in new[] { "BrasilAPI", "CNPJA", "CNPJWS", "ReceitaWS" })
            foreach (var status in new[] { 400, 404, 429, 500, 503 })
                yield return new object[] { name, status };
        }

        [Theory]
        [MemberData(nameof(HttpErrors))]
        public async Task HttpFailure_DisposesResponseContentAndStream(string name, int status)
        {
            using var response = new TrackedHttpResponse((HttpStatusCode)status, "erro HTTP simulado");
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            var error = await Assert.ThrowsAsync<ProviderException>(() => CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            Assert.IsType<HttpRequestException>(error.InnerException);
            AssertDisposed(response);
        }

        [Theory]
        [MemberData(nameof(HttpErrors))]
        public async Task UnavailableStatus_DisposesResponseContentAndStream(string name, int status)
        {
            using var response = new TrackedHttpResponse((HttpStatusCode)status, "indisponível");
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            Assert.False(await CreateProvider(name, http).IsAvailableAsync());

            AssertDisposed(response);
        }

        public static IEnumerable<object[]> InvalidJson()
        {
            foreach (var name in new[] { "BrasilAPI", "CNPJA", "CNPJWS", "ReceitaWS" })
            {
                yield return new object[] { name, "{" };
                var property = name == "CNPJA" ? "taxId" : name == "CNPJWS" ? "estabelecimento" : "cnpj";
                yield return new object[] { name, "{\"" + property + "\":[]}" };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidJson))]
        public async Task InvalidJsonOrWrongType_DisposesResponseContentAndStream(string name, string json)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, json);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            var error = await Assert.ThrowsAsync<ProviderException>(() => CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            Assert.IsType<System.Text.Json.JsonException>(error.InnerException);
            AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task NullJson_DisposesResponseBeforeReturningNoData(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, "null");
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            Assert.Null(await CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI", "cnaes_secundarios")]
        [InlineData("CNPJA", "sideActivities")]
        [InlineData("CNPJWS", "atividades_secundarias")]
        [InlineData("ReceitaWS", "atividades_secundarias")]
        public async Task NullListMember_DisposesResponseContentAndStream(string name, string property)
        {
            var payload = name == "CNPJWS"
                ? "{\"estabelecimento\":{\"cnpj\":\"12345678000195\",\"" + property + "\":[null]}}"
                : "{\"" + (name == "CNPJA" ? "taxId" : "cnpj") + "\":\"12345678000195\",\"" + property + "\":[null]}";
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, payload);
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            var data = await CreateProvider(name, http).GetCnpjDataAsync("12345678000195");
            Assert.NotNull(data);

            Assert.Empty(data.AtividadesSecundarias);
            AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task ContentBufferingFailure_DisposesResourcesBeforeResponseIsReturnedByHttpClient(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, new FailingTrackedContent());
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            await Assert.ThrowsAsync<ProviderException>(() => CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task InvalidCharset_DisposesResponseWhenReadAsStringFails(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, SuccessJson(name));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "encoding-inexistente" };
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            await Assert.ThrowsAsync<ProviderException>(() => CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            AssertDisposed(response);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task AvailabilityWithoutResponse_DoesNotDisposeSharedClient(string name)
        {
            using var handler = new TrackedHandler(_ => throw new HttpRequestException("falha de transporte simulada"));
            using var http = new HttpClient(handler);

            Assert.False(await CreateProvider(name, http).IsAvailableAsync());

            Assert.Equal(0, handler.DisposeCalls);
        }

        [Theory]
        [InlineData("http")]
        [InlineData("json")]
        [InlineData("no-data")]
        public async Task Fallback_DisposesFailedResponseBeforeConsultingNextProvider(string failure)
        {
            using var first = new TrackedHttpResponse(failure == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK,
                failure == "json" ? "{" : "null");
            using var second = new TrackedHttpResponse(HttpStatusCode.OK, SuccessJson("ReceitaWS"));
            using var handler = new TrackedHandler(request =>
            {
                if (request.RequestUri!.Host == "publica.cnpj.ws") return first;
                AssertDisposed(first);
                return second;
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);

            var result = await client.GetAsync("12345678000195");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("ReceitaWS", result.Data.Provedor);
            AssertDisposed(second);
            Assert.Equal(2, handler.Requests);
        }

        [Fact]
        public async Task AllProvidersFail_DisposesAllResponsesAndKeepsFailureDetails()
        {
            var responses = new List<TrackedHttpResponse>();
            using var handler = new TrackedHandler(_ =>
            {
                var response = new TrackedHttpResponse(HttpStatusCode.InternalServerError, "falha simulada");
                responses.Add(response);
                return response;
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);
            try
            {
                var result = await client.GetAsync("12345678000195");
                Assert.False(result.Success);
                Assert.Equal(4, result.Errors.Count);
                Assert.All(result.Errors, error => Assert.Contains("falha simulada", error.ErrorMessage));
                Assert.Equal(4, responses.Count);
                Assert.All(responses, AssertDisposed);
            }
            finally
            {
                foreach (var response in responses) response.Dispose();
            }
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task SuccessfulQuery_DisposesResponseContentAndStream(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, SuccessJson(name));
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            Assert.NotNull(await CreateProvider(name, http).GetCnpjDataAsync("12345678000195"));

            AssertDisposed(response); // Verificado antes da limpeza do fixture.
            Assert.Equal(0, handler.DisposeCalls);
        }

        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task AvailabilityCheck_DisposesResponseContentAndStream(string name)
        {
            using var response = new TrackedHttpResponse(HttpStatusCode.OK, "disponibilidade simulada");
            using var handler = new TrackedHandler(_ => response);
            using var http = new HttpClient(handler);

            Assert.True(await CreateProvider(name, http).IsAvailableAsync());

            AssertDisposed(response);
            Assert.Equal(0, handler.DisposeCalls);
        }

        internal static void AssertDisposed(TrackedHttpResponse response)
        {
            Assert.Equal(1, response.DisposeCalls);
            Assert.Equal(1, response.TrackedContent.DisposeCalls);
            Assert.True(response.TrackedContent.Source.IsDisposed);
        }

        internal static ICnpjProvider CreateProvider(string name, HttpClient http)
        {
            var limiter = new SlidingWindowRateLimiter(10);
            switch (name)
            {
                case "BrasilAPI": return new BrasilAPIProvider(http, limiter);
                case "CNPJA": return new CNPJAProvider(http, limiter);
                case "CNPJWS": return new CNPJWSProvider(http, limiter);
                case "ReceitaWS": return new ReceitaWSProvider(http, limiter);
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }

        internal static string SuccessJson(string name)
        {
            switch (name)
            {
                case "BrasilAPI": return "{\"cnpj\":\"12345678000195\"}";
                case "CNPJA": return "{\"taxId\":\"12345678000195\"}";
                case "CNPJWS": return "{\"estabelecimento\":{\"cnpj\":\"12345678000195\"}}";
                case "ReceitaWS": return "{\"status\":\"OK\",\"cnpj\":\"12345678000195\"}";
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
        }
    }
}
