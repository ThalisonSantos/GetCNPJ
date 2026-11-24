using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace GetCNPJ.Tests
{
    public class HttpClientOwnershipTests
    {
        [Fact]
        public async Task ExternalClient_RemainsUsableAfterRepeatedCnpjClientDispose()
        {
            using var handler = new TrackedHandler(_ => new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson("BrasilAPI")));
            using var http = new TrackedHttpClient(handler) { Timeout = TimeSpan.FromSeconds(17) };
            var client = new CnpjClient(http, new CnpjClientOptions { Timeout = TimeSpan.Zero });
            client.Dispose();
            client.Dispose();

            using var response = await http.GetAsync("https://example.invalid/reuso");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(0, http.DisposeCalls);
            Assert.Equal(0, handler.DisposeCalls);
            Assert.Equal(TimeSpan.FromSeconds(17), http.Timeout);
        }

        [Fact]
        public async Task TwoWrappers_ReuseExternalClientWithoutTakingOwnership()
        {
            var responses = new List<TrackedHttpResponse>();
            using var handler = new TrackedHandler(_ =>
            {
                var response = new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson("BrasilAPI"));
                responses.Add(response);
                return response;
            });
            using var http = new TrackedHttpClient(handler);
            using var first = new CnpjClient(http);
            using var second = new CnpjClient(http);
            try
            {
                Assert.True((await first.GetFromProviderAsync("12345678000195", "BrasilAPI")).Success);
                first.Dispose();
                Assert.True((await second.GetFromProviderAsync("12345678000195", "BrasilAPI")).Success);
                Assert.Equal(0, http.DisposeCalls);
                Assert.Equal(2, handler.Requests);
                Assert.All(responses, HttpResourceDisposalTests.AssertDisposed);
            }
            finally
            {
                foreach (var response in responses) response.Dispose();
            }
        }

        [Fact]
        public async Task OwnedClient_IsCreatedOnceReusedAndDisposedWithItsHandler()
        {
            var responses = new List<TrackedHttpResponse>();
            using var handler = new TrackedHandler(_ =>
            {
                var response = new TrackedHttpResponse(HttpStatusCode.OK, HttpResourceDisposalTests.SuccessJson("BrasilAPI"));
                responses.Add(response);
                return response;
            });
            using var http = new TrackedHttpClient(handler);
            var creations = 0;
            using var client = new CnpjClient(null, new CnpjClientOptions { Timeout = TimeSpan.FromSeconds(11) }, () =>
            {
                creations++;
                return http;
            });
            try
            {
                for (var i = 0; i < 3; i++)
                    Assert.True((await client.GetFromProviderAsync("12345678000195", "BrasilAPI")).Success);

                Assert.Equal(1, creations);
                Assert.Equal(3, handler.Requests);
                Assert.Equal(TimeSpan.FromSeconds(11), http.Timeout);
                Assert.Equal(0, http.DisposeCalls);
                Assert.All(responses, HttpResourceDisposalTests.AssertDisposed);

                client.Dispose();
                client.Dispose();
                Assert.Equal(1, http.DisposeCalls);
                Assert.Equal(1, handler.DisposeCalls);
                await Assert.ThrowsAsync<ObjectDisposedException>(() => http.GetAsync("https://example.invalid/descartado"));
            }
            finally
            {
                foreach (var response in responses) response.Dispose();
            }
        }

        [Fact]
        public async Task RepeatedConcurrentDispose_DisposesOwnedClientOnlyOnce()
        {
            using var handler = new TrackedHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new TrackedHttpClient(handler);
            using var client = new CnpjClient(null, null, () => http);

            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(client.Dispose)));

            Assert.Equal(1, http.DisposeCalls);
            Assert.Equal(1, handler.DisposeCalls);
        }

        [Fact]
        public void ConstructorWithoutProviders_DisposesOwnedClientAndHandler()
        {
            using var handler = new TrackedHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new TrackedHttpClient(handler);

            Assert.Throws<InvalidOperationException>(() => new CnpjClient(null, NoProviders(), () => http));

            Assert.Equal(1, http.DisposeCalls);
            Assert.Equal(1, handler.DisposeCalls);
        }

        [Fact]
        public async Task ConstructorWithoutProviders_DoesNotDisposeExternalClient()
        {
            using var handler = new TrackedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            using var http = new TrackedHttpClient(handler);

            Assert.Throws<InvalidOperationException>(() => new CnpjClient(http, NoProviders()));

            Assert.Equal(0, http.DisposeCalls);
            Assert.Equal(0, handler.DisposeCalls);
            using var response = await http.GetAsync("https://example.invalid/reuso");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-2L)]
        [InlineData(long.MaxValue)]
        public void InvalidOwnedTimeout_DisposesClientEvenWhenPropertySetterThrows(long ticks)
        {
            using var handler = new TrackedHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new TrackedHttpClient(handler);

            Assert.Throws<ArgumentOutOfRangeException>(() => new CnpjClient(null,
                new CnpjClientOptions { Timeout = TimeSpan.FromTicks(ticks) }, () => http));

            Assert.Equal(1, http.DisposeCalls);
            Assert.Equal(1, handler.DisposeCalls);
        }

        [Fact]
        public void ExternalClient_DoesNotInvokeOwnedFactory()
        {
            using var handler = new TrackedHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new TrackedHttpClient(handler);
            using var client = new CnpjClient(http, null, () => throw new InvalidOperationException("fábrica não deveria ser usada"));
            client.Dispose();

            Assert.Equal(0, http.DisposeCalls);
            Assert.Equal(0, handler.DisposeCalls);
        }

        private static CnpjClientOptions NoProviders()
        {
            return new CnpjClientOptions
            {
                EnableBrasilAPI = false, EnableCNPJA = false,
                EnableCNPJWS = false, EnableReceitaWS = false
            };
        }
    }
}
