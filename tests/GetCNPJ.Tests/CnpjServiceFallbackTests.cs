using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Interfaces;
using GetCNPJ.Models;
using GetCNPJ.Services;
using Xunit;

namespace GetCNPJ.Tests
{
    public class CnpjServiceFallbackTests
    {
        [Fact]
        public async Task MultipleFailuresBeforeSuccess_PreservePriorityAndStopAfterSuccess()
        {
            var calls = new List<string>();
            var expected = new CnpjData { RazaoSocial = "EMPRESA FICTICIA" };
            var first = new FakeProvider("A", 1, calls, _ => throw new HttpRequestException("falha simulada"));
            var second = new FakeProvider("B", 2, calls, _ => Task.FromResult<CnpjData>(null!));
            var third = new FakeProvider("C", 3, calls, _ => Task.FromResult(expected));
            var unused = new FakeProvider("D", 4, calls, _ => throw new InvalidOperationException("não deveria consultar"));
            var service = new CnpjService(new[] { unused, third, second, first });

            var result = await service.GetCnpjAsync("12345678000195");

            Assert.True(result.Success);
            Assert.Same(expected, result.Data);
            Assert.Equal(new[] { "A", "B", "C" }, calls);
            Assert.Equal(0, unused.CallCount);
        }

        [Fact]
        public async Task AllFailures_PreserveProviderNamesMessagesAndOriginalExceptions()
        {
            var calls = new List<string>();
            var firstError = new HttpRequestException("erro HTTP simulado");
            var lastError = new InvalidOperationException("erro de provedor simulado");
            var first = new FakeProvider("A", 1, calls, _ => throw firstError);
            var second = new FakeProvider("B", 2, calls, _ => Task.FromResult<CnpjData>(null!));
            var third = new FakeProvider("C", 3, calls, _ => throw lastError);

            var result = await new CnpjService(new[] { third, first, second }).GetCnpjAsync("12345678000195");

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Contains("3", result.ErrorMessage);
            Assert.Equal(new[] { "A", "B", "C" }, calls);
            Assert.Equal(calls, result.FailedProviders);
            Assert.Equal(3, result.Errors.Count);
            Assert.Same(firstError, result.Errors[0].Exception);
            Assert.Equal(firstError.Message, result.Errors[0].ErrorMessage);
            Assert.Null(result.Errors[1].Exception);
            Assert.Equal("CNPJ não encontrado", result.Errors[1].ErrorMessage);
            Assert.Same(lastError, result.Errors[2].Exception);
            Assert.Equal(lastError.Message, result.Errors[2].ErrorMessage);
        }

        [Theory]
        [InlineData("operation")]
        [InlineData("task")]
        [InlineData("inner-timeout")]
        [InlineData("http")]
        [InlineData("internal-token")]
        public async Task AllTimeouts_AreRecordedAndEachProviderIsCalledOnlyOnce(string shape)
        {
            var calls = new List<string>();
            var errors = Enumerable.Range(0, 3).Select(_ => CreateTimeout(shape)).ToArray();
            var providers = Enumerable.Range(0, 3).Select(i =>
                new FakeProvider("P" + i, i, calls, _ => throw errors[i])).Reverse().ToArray();

            var result = await new CnpjService(providers).GetCnpjAsync("12345678000195");

            Assert.False(result.Success);
            Assert.Equal(new[] { "P0", "P1", "P2" }, calls);
            Assert.Equal(3, result.Errors.Count);
            for (var i = 0; i < 3; i++)
            {
                Assert.Same(errors[i], result.Errors[i].Exception);
                Assert.Equal(errors[i].Message, result.Errors[i].ErrorMessage);
                Assert.Equal("P" + i, result.Errors[i].ProviderName);
            }
            Assert.All(providers, p => Assert.Equal(1, p.CallCount));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancellationBeforeQuery_DoesNotInvokeAnyProvider(bool directed)
        {
            var calls = new List<string>();
            // Intencionalmente não observa o token; o serviço deve impedir a chamada.
            var provider = new FakeProvider("A", 1, calls, _ => Task.FromResult(new CnpjData()));
            var service = new CnpjService(new[] { provider });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => directed
                ? service.GetCnpjFromProviderAsync("12345678000195", "A", cancellation.Token)
                : service.GetCnpjAsync("12345678000195", cancellation.Token));

            Assert.Empty(calls);
        }

        [Theory]
        [InlineData(false, "null")]
        [InlineData(false, "success")]
        [InlineData(false, "http")]
        [InlineData(false, "operation")]
        [InlineData(false, "task")]
        [InlineData(true, "null")]
        [InlineData(true, "success")]
        [InlineData(true, "http")]
        [InlineData(true, "operation")]
        [InlineData(true, "task")]
        public async Task CancellationAtEndOfAttempt_TakesPrecedenceOverOtherOutcomes(bool directed, string outcome)
        {
            var calls = new List<string>();
            using var cancellation = new CancellationTokenSource();
            var first = new FakeProvider("A", 1, calls, token =>
            {
                Assert.Equal(cancellation.Token, token);
                cancellation.Cancel(); // Entre o trabalho deste provedor e a próxima tentativa.
                if (outcome == "success") return Task.FromResult(new CnpjData());
                if (outcome == "null") return Task.FromResult<CnpjData>(null!);
                throw CreateTimeout(outcome);
            });
            var second = new FakeProvider("B", 2, calls, _ => Task.FromResult(new CnpjData()));
            var service = new CnpjService(new[] { second, first });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => directed
                ? service.GetCnpjFromProviderAsync("12345678000195", "A", cancellation.Token)
                : service.GetCnpjAsync("12345678000195", cancellation.Token));

            Assert.Equal(new[] { "A" }, calls);
            Assert.Equal(0, second.CallCount);
        }

        [Theory]
        [InlineData("operation")]
        [InlineData("task")]
        [InlineData("inner-timeout")]
        [InlineData("http")]
        [InlineData("internal-token")]
        public async Task DirectedQuery_TimeoutReturnsOneErrorWithoutFallback(string shape)
        {
            var calls = new List<string>();
            var error = CreateTimeout(shape);
            var unused = new FakeProvider("A", 1, calls, _ => Task.FromResult(new CnpjData()));
            var selected = new FakeProvider("B", 2, calls, _ => throw error);
            var result = await new CnpjService(new[] { unused, selected })
                .GetCnpjFromProviderAsync("12345678000195", "b");

            Assert.False(result.Success);
            Assert.Null(result.Data);
            var failure = Assert.Single(result.Errors);
            Assert.Equal("B", failure.ProviderName);
            Assert.Equal(error.Message, failure.ErrorMessage);
            Assert.Same(error, failure.Exception);
            Assert.Equal(new[] { "B" }, calls);
            Assert.Equal(0, unused.CallCount);
        }

        [Fact]
        public async Task EqualPriorities_PreserveInputOrderAndNeverCallAfterSuccess()
        {
            var calls = new List<string>();
            var first = new FakeProvider("B", 1, calls, _ => Task.FromResult<CnpjData>(null!));
            var second = new FakeProvider("A", 1, calls, _ => Task.FromResult(new CnpjData()));
            var last = new FakeProvider("C", 1, calls, _ => Task.FromResult(new CnpjData()));

            Assert.True((await new CnpjService(new[] { first, second, last }).GetCnpjAsync("12345678000195")).Success);

            Assert.Equal(new[] { "B", "A" }, calls);
        }

        internal static Exception CreateTimeout(string shape)
        {
            switch (shape)
            {
                case "operation": return new OperationCanceledException("cancelamento interno simulado");
                case "task": return new TaskCanceledException("timeout simulado");
                case "inner-timeout": return new TaskCanceledException("timeout simulado", new TimeoutException("limite HTTP"));
                case "http": return new HttpRequestException("timeout HTTP simulado");
                case "internal-token": return new OperationCanceledException("token interno", new CancellationToken(true));
                default: throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        private sealed class FakeProvider : ICnpjProvider
        {
            private readonly Func<CancellationToken, Task<CnpjData>> _get;
            private readonly List<string> _calls;
            public string ProviderName { get; }
            public int Priority { get; }
            public int CallCount { get; private set; }

            public FakeProvider(string name, int priority, List<string> calls, Func<CancellationToken, Task<CnpjData>> get)
            {
                ProviderName = name;
                Priority = priority;
                _calls = calls;
                _get = get;
            }

            public Task<CnpjData> GetCnpjDataAsync(string cnpj, CancellationToken cancellationToken = default)
            {
                CallCount++;
                _calls.Add(ProviderName);
                return _get(cancellationToken);
            }

            public Task<bool> IsAvailableAsync() => Task.FromResult(true);
        }
    }
}
