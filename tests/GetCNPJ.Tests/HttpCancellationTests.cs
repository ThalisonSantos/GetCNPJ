using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GetCNPJ.Tests
{
    public class HttpCancellationTests
    {
        private readonly ITestOutputHelper _output;
        public HttpCancellationTests(ITestOutputHelper output) { _output = output; }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConsumerCancellationDuringHttp_StopsWithoutOtherProviders(bool directed)
        {
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var handler = new AsyncStubHttpMessageHandler((request, token) =>
            {
                entered.TrySetResult(true);
                return WaitForCancellationAsync(token);
            });
            using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var client = new CnpjClient(http);
            using var cancellation = new CancellationTokenSource();
            var query = directed
                ? client.GetFromProviderAsync("12345678000195", "CNPJWS", cancellation.Token)
                : client.GetAsync("12345678000195", cancellation.Token);
            await AsyncTest.WithTimeout(entered.Task);
            cancellation.Cancel();

            await AsyncTest.WithTimeout(Assert.ThrowsAnyAsync<OperationCanceledException>(() => query));

            Assert.Equal(1, handler.RequestCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConsumerCancellationBeforeHttp_DoesNotSendAnyRequest(bool directed)
        {
            using var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => directed
                ? client.GetFromProviderAsync("12345678000195", "CNPJWS", cancellation.Token)
                : client.GetAsync("12345678000195", cancellation.Token));

            Assert.Equal(0, handler.RequestCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NativeHttpClientTimeout_IsNotConsumerCancellation(bool directed)
        {
            var hosts = new List<string>();
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"status\":\"OK\",\"cnpj\":\"12345678000195\"}") };
            using var handler = new AsyncStubHttpMessageHandler((request, token) =>
            {
                hosts.Add(request.RequestUri!.Host);
                return request.RequestUri.Host == "publica.cnpj.ws"
                    ? WaitForCancellationAsync(token) : Task.FromResult(response);
            });
            // Exerce o timer real de HttpClient sobre um handler local; não há rede ou asserção de duração.
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
            using var client = new CnpjClient(http);
            using var consumer = new CancellationTokenSource();
            var query = directed
                ? client.GetFromProviderAsync("12345678000195", "CNPJWS", consumer.Token)
                : client.GetAsync("12345678000195", consumer.Token);
            await AsyncTest.WithTimeout(query);
            var result = await query;

            Assert.False(consumer.IsCancellationRequested);
            if (directed)
            {
                Assert.False(result.Success);
                var error = Assert.Single(result.Errors).Exception;
                Assert.NotNull(error);
                _output.WriteLine("Exceção nativa: {0}; interna: {1}", error.GetType().FullName, error.InnerException?.GetType().FullName ?? "nenhuma");
                Assert.Equal(new[] { "publica.cnpj.ws" }, hosts);
            }
            else
            {
                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Equal("ReceitaWS", result.Data.Provedor);
                Assert.Equal(new[] { "publica.cnpj.ws", "receitaws.com.br" }, hosts);
            }
        }

        private static async Task<HttpResponseMessage> WaitForCancellationAsync(CancellationToken token)
        {
            Assert.True(token.CanBeCanceled);
            await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
            throw new InvalidOperationException("A espera deveria terminar com cancelamento");
        }
    }
}
