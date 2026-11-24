using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GetCNPJ.RateLimiter
{
    // Ponto de substituição interno para testes sem depender do relógio de parede ou de sleeps.
    internal interface IRateLimiterTime
    {
        TimeSpan Elapsed { get; }
        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    internal sealed class StopwatchRateLimiterTime : IRateLimiterTime
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}
