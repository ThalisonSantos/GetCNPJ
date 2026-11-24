using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Interfaces;
using GetCNPJ.Models;
using GetCNPJ.Providers.Base;
using GetCNPJ.Providers.BrasilAPI;
using GetCNPJ.Providers.CNPJA;
using GetCNPJ.Providers.CNPJWS;
using GetCNPJ.Providers.ReceitaWS;
using GetCNPJ.RateLimiter;
using Xunit;

namespace GetCNPJ.Tests
{
    public class RateLimiterIntegrationTests
    {
        [Theory]
        [InlineData("BrasilAPI")]
        [InlineData("CNPJA")]
        [InlineData("CNPJWS")]
        [InlineData("ReceitaWS")]
        public async Task ConcreteProviders_RegisterExactlyOnceWithTheBuiltInLimiter(string name)
        {
            var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromMinutes(1), new ManualRateLimiterTime());
            var json = name == "CNPJWS" ? "{\"estabelecimento\":{\"cnpj\":\"12345678000195\"}}"
                : name == "CNPJA" ? "{\"taxId\":\"12345678000195\"}"
                : "{\"cnpj\":\"12345678000195\"}";
            using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            using var handler = new StubHttpMessageHandler(_ => response);
            using var http = new HttpClient(handler);
            ICnpjProvider provider;
            switch (name)
            {
                case "BrasilAPI": provider = new BrasilAPIProvider(http, limiter); break;
                case "CNPJA": provider = new CNPJAProvider(http, limiter); break;
                case "CNPJWS": provider = new CNPJWSProvider(http, limiter); break;
                default: provider = new ReceitaWSProvider(http, limiter); break;
            }

            Assert.NotNull(await provider.GetCnpjDataAsync("12345678000195"));

            Assert.Equal(1, handler.RequestCount);
            Assert.Equal(2, limiter.GetAvailableRequests(name));
            Assert.Equal(3, limiter.GetAvailableRequests("Outro"));
        }

        [Fact]
        public async Task OptionalAtomicImplementation_IsUsedWithoutLegacyCallsOrDuplicateRegistration()
        {
            var limiter = new AtomicLimiter();
            using var http = new HttpClient();
            var provider = new ProbeProvider(http, limiter);
            using var cancellation = new CancellationTokenSource();

            await provider.GetCnpjDataAsync("12345678000195", cancellation.Token);

            Assert.Equal(1, limiter.AcquireCalls);
            Assert.Equal(0, limiter.WaitCalls);
            Assert.Equal(0, limiter.RecordCalls);
            Assert.Equal(cancellation.Token, limiter.Token);
            Assert.Equal("Probe", limiter.Provider);
            Assert.Equal(1, provider.FetchCalls);
        }

        [Fact]
        public async Task ExistingIRateLimiterImplementation_WaitsThenRecordsOnceWithoutNewInterface()
        {
            var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var limiter = new LegacyLimiter { Wait = _ => permission.Task };
            using var http = new HttpClient();
            var provider = new ProbeProvider(http, limiter);
            using var cancellation = new CancellationTokenSource();

            var request = provider.GetCnpjDataAsync("12345678000195", cancellation.Token);
            Assert.False(request.IsCompleted);
            Assert.Equal(0, limiter.RecordCalls);
            Assert.Equal(0, provider.FetchCalls);
            permission.SetResult(true);
            await AsyncTest.WithTimeout(request);

            Assert.Equal(new[] { "wait", "record" }, limiter.Calls);
            Assert.Equal(1, limiter.RecordCalls);
            Assert.Equal(cancellation.Token, limiter.Token);
            Assert.Equal("Probe", limiter.Provider);
            Assert.Equal(1, provider.FetchCalls);
        }

        [Fact]
        public async Task ExistingImplementation_CancellationDoesNotRegisterOrFetch()
        {
            var limiter = new LegacyLimiter { Wait = token => Task.Delay(Timeout.Infinite, token) };
            using var http = new HttpClient();
            var provider = new ProbeProvider(http, limiter);
            using var cancellation = new CancellationTokenSource();
            var request = provider.GetCnpjDataAsync("12345678000195", cancellation.Token);
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);

            Assert.Equal(1, limiter.WaitCalls);
            Assert.Equal(0, limiter.RecordCalls);
            Assert.Equal(0, provider.FetchCalls);
        }

        [Fact]
        public async Task ProviderCancellationWithAtomicLimiter_DoesNotFetchOrRegisterAgain()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromMinutes(1), time);
            using var http = new HttpClient();
            var provider = new ProbeProvider(http, limiter);
            await limiter.AcquireAsync("Probe");
            using var cancellation = new CancellationTokenSource();
            var request = provider.GetCnpjDataAsync("12345678000195", cancellation.Token);
            Assert.False(request.IsCompleted);
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);

            Assert.Equal(0, provider.FetchCalls);
            time.Advance(TimeSpan.FromMinutes(1));
            Assert.Equal(1, limiter.GetAvailableRequests("Probe"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CnpjClient_RejectsInvalidConfiguredLimit(int limit)
        {
            using var http = new HttpClient();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CnpjClient(http, new CnpjClientOptions { MaxRequestsPerMinute = limit }));
        }

        private class LegacyLimiter : IRateLimiter
        {
            public int WaitCalls;
            public int RecordCalls;
            public CancellationToken Token;
            public string? Provider;
            public readonly List<string> Calls = new List<string>();
            public Func<CancellationToken, Task> Wait = _ => Task.CompletedTask;

            public Task WaitIfNeededAsync(string providerName, CancellationToken cancellationToken = default)
            {
                WaitCalls++;
                Calls.Add("wait");
                Token = cancellationToken;
                Provider = providerName;
                return Wait(cancellationToken);
            }

            public void RecordRequest(string providerName) { RecordCalls++; Calls.Add("record"); }
            public void Reset(string providerName) { }
            public int GetAvailableRequests(string providerName) => 1;
        }

        private sealed class AtomicLimiter : LegacyLimiter, IAtomicRateLimiter
        {
            public int AcquireCalls;
            public Task AcquireAsync(string providerName, CancellationToken cancellationToken = default)
            {
                AcquireCalls++;
                Provider = providerName;
                Token = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class ProbeProvider : CnpjProviderBase
        {
            public int FetchCalls;
            public override string ProviderName => "Probe";
            public override int Priority => 1;
            protected override string BaseUrl => "https://example.invalid/";

            public ProbeProvider(HttpClient http, IRateLimiter limiter) : base(http, limiter) { }

            protected override Task<CnpjData?> FetchDataAsync(string cnpj, CancellationToken cancellationToken)
            {
                FetchCalls++;
                return Task.FromResult<CnpjData?>(new CnpjData { Cnpj = cnpj });
            }
        }
    }
}
