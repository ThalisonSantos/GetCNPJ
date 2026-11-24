using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Interfaces;
using GetCNPJ.Providers.CNPJWS;
using GetCNPJ.Providers.ReceitaWS;
using GetCNPJ.RateLimiter;
using GetCNPJ.Services;
using Xunit;

namespace GetCNPJ.Tests
{
    public class TimeoutFallbackTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancellationWhenLimiterReturns_DoesNotReachHttp(bool atomic)
        {
            using var cancellation = new CancellationTokenSource();
            CancellingLimiter limiter = atomic
                ? new AtomicCancellingLimiter(cancellation.Cancel) : new CancellingLimiter(cancellation.Cancel);
            using var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new HttpClient(handler);
            var service = new CnpjService(new ICnpjProvider[] { new CNPJWSProvider(http, limiter) });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetCnpjAsync("12345678000195", cancellation.Token));

            Assert.Equal(0, handler.RequestCount);
            Assert.Equal(0, limiter.RecordCalls);
            Assert.Equal(1, limiter.Admissions);
        }

        [Fact]
        public async Task GetAsync_MultipleHttpFailures_ReachesLastProviderInPriorityOrderWithoutRetries()
        {
            var hosts = new List<string>();
            using var unavailable = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("falha simulada") };
            using var success = new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"taxId\":\"12345678000195\",\"company\":{\"name\":\"EMPRESA FICTICIA\"}}") };
            using var handler = new StubHttpMessageHandler(request =>
            {
                hosts.Add(request.RequestUri!.Host);
                switch (request.RequestUri.Host)
                {
                    case "publica.cnpj.ws": throw new HttpRequestException("falha HTTP simulada");
                    case "receitaws.com.br": throw new TaskCanceledException("timeout HTTP simulado");
                    case "brasilapi.com.br": return unavailable;
                    default: return success;
                }
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);

            var result = await client.GetAsync("12345678000195");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("CNPJA", result.Data.Provedor);
            Assert.Equal(new[] { "publica.cnpj.ws", "receitaws.com.br", "brasilapi.com.br", "open.cnpja.com" }, hosts);
            Assert.Equal(4, handler.RequestCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsync_AllHttpProvidersTimeOut_ReturnsAllFailures(bool taskCanceled)
        {
            using var handler = new StubHttpMessageHandler(_ => throw (taskCanceled
                ? new TaskCanceledException("timeout HTTP simulado")
                : new OperationCanceledException("timeout HTTP simulado")));
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);

            var result = await client.GetAsync("12345678000195");

            Assert.False(result.Success);
            Assert.Equal(new[] { "CNPJWS", "ReceitaWS", "BrasilAPI", "CNPJA" }, result.FailedProviders);
            Assert.Equal(4, result.Errors.Count);
            Assert.All(result.Errors, e => Assert.IsAssignableFrom<OperationCanceledException>(e.Exception));
            Assert.Equal(4, handler.RequestCount);
        }

        [Theory]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        public async Task GetFromProviderAsync_HttpTimeout_DoesNotConsultOtherProviders(string name)
        {
            using var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("timeout simulado"));
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);

            var result = await client.GetFromProviderAsync("12345678000195", name);

            Assert.False(result.Success);
            Assert.Equal(name, Assert.Single(result.Errors).ProviderName);
            Assert.IsAssignableFrom<OperationCanceledException>(result.Errors[0].Exception);
            Assert.Equal(1, handler.RequestCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConsumerCancellationWhileRateLimited_StopsWithoutHttpOrFallback(bool directed)
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromHours(1), time);
            await limiter.AcquireAsync("CNPJWS");
            using var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP não esperado"));
            using var http = new HttpClient(handler);
            var service = new CnpjService(new ICnpjProvider[]
            {
                new ReceitaWSProvider(http, limiter), new CNPJWSProvider(http, limiter)
            });
            using var cancellation = new CancellationTokenSource();
            var query = directed
                ? service.GetCnpjFromProviderAsync("12345678000195", "CNPJWS", cancellation.Token)
                : service.GetCnpjAsync("12345678000195", cancellation.Token);
            await AsyncTest.WithTimeout(time.WaitForDelayCountAsync(1));
            cancellation.Cancel();

            await AsyncTest.WithTimeout(Assert.ThrowsAnyAsync<OperationCanceledException>(() => query));

            Assert.Equal(0, handler.RequestCount);
            Assert.Equal(0, limiter.GetAvailableRequests("CNPJWS"));
            Assert.Equal(1, limiter.GetAvailableRequests("ReceitaWS"));
            Assert.Equal(0, time.ActiveDelays);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsync_TimeoutOfFirstProvider_AllowsSecondToSucceed(bool taskCanceled)
        {
            var hosts = new List<string>();
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"OK\",\"cnpj\":\"12345678000195\",\"nome\":\"EMPRESA FICTICIA\"}")
            };
            using var handler = new StubHttpMessageHandler(request =>
            {
                hosts.Add(request.RequestUri!.Host);
                if (request.RequestUri.Host == "publica.cnpj.ws")
                    throw taskCanceled
                        ? new TaskCanceledException("timeout HTTP simulado")
                        : new OperationCanceledException("timeout HTTP simulado");
                return response;
            });
            using var http = new HttpClient(handler);
            using var client = new CnpjClient(http);

            var result = await client.GetAsync("12345678000195", CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("ReceitaWS", result.Data.Provedor);
            Assert.Equal(new[] { "publica.cnpj.ws", "receitaws.com.br" }, hosts);
            Assert.Equal(2, handler.RequestCount);
        }

        private class CancellingLimiter : IRateLimiter
        {
            protected readonly Action Cancel;
            public int RecordCalls;
            public int Admissions;
            public CancellingLimiter(Action cancel) { Cancel = cancel; }
            public Task WaitIfNeededAsync(string name, CancellationToken token = default)
            {
                Admissions++;
                Cancel();
                return Task.CompletedTask;
            }
            public void RecordRequest(string name) { RecordCalls++; }
            public void Reset(string name) { }
            public int GetAvailableRequests(string name) => 1;
        }

        private sealed class AtomicCancellingLimiter : CancellingLimiter, IAtomicRateLimiter
        {
            public AtomicCancellingLimiter(Action cancel) : base(cancel) { }
            public Task AcquireAsync(string name, CancellationToken token = default)
            {
                Admissions++;
                Cancel();
                return Task.CompletedTask;
            }
        }
    }
}
