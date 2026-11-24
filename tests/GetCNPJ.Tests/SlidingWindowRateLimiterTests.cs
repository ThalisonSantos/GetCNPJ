using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.RateLimiter;
using Xunit;

namespace GetCNPJ.Tests
{
    public class SlidingWindowRateLimiterTests
    {
        [Fact]
        public async Task AcquireAsync_WithSimultaneousRequests_AdmitsOnlyTheConfiguredLimit()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(7, TimeSpan.FromMinutes(1), time);
            using var cancellation = new CancellationTokenSource();
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requests = new ConcurrentBag<Task>();
            var count = 0;
            var workers = Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
            {
                await start.Task;
                var request = limiter.AcquireAsync("A", cancellation.Token);
                requests.Add(request);
                if (Interlocked.Increment(ref count) == 100) attempted.TrySetResult(true);
                await request;
            })).ToArray();

            try
            {
                start.SetResult(true);
                await AsyncTest.WithTimeout(attempted.Task);
                Assert.Equal(7, requests.Count(t => t.Status == TaskStatus.RanToCompletion));
                Assert.Equal(0, limiter.GetAvailableRequests("A"));
                Assert.Equal(93, time.ActiveDelays);
            }
            finally
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(workers));
            }
            Assert.Equal(0, time.ActiveDelays);
        }

        [Fact]
        public async Task AcquireAsync_ExpiresEachRequestAtItsOwnSlidingBoundary()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromSeconds(10), time);
            await limiter.AcquireAsync("A"); // t = 0
            time.Advance(TimeSpan.FromSeconds(4));
            await limiter.AcquireAsync("A"); // t = 4
            var waiting = limiter.AcquireAsync("A");
            Assert.False(waiting.IsCompleted);
            Assert.Equal(TimeSpan.FromSeconds(6), time.LastDelay);

            time.Advance(TimeSpan.FromMilliseconds(5999));
            Assert.False(waiting.IsCompleted);
            Assert.Equal(0, limiter.GetAvailableRequests("A"));
            time.Advance(TimeSpan.FromMilliseconds(1)); // t = 10: somente a primeira expira.
            await AsyncTest.WithTimeout(waiting);
            Assert.Equal(0, limiter.GetAvailableRequests("A"));

            time.Advance(TimeSpan.FromSeconds(4)); // t = 14: a segunda expira.
            Assert.Equal(1, limiter.GetAvailableRequests("A"));
            time.Advance(TimeSpan.FromSeconds(6));
            Assert.Equal(2, limiter.GetAvailableRequests("A"));
        }

        [Fact]
        public async Task AcquireAsync_RechecksCapacityWhenSeveralWaitersWakeTogether()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromSeconds(10), time);
            using var cancellation = new CancellationTokenSource();
            await limiter.AcquireAsync("A");
            await limiter.AcquireAsync("A");
            var waiting = Enumerable.Range(0, 6).Select(_ => limiter.AcquireAsync("A", cancellation.Token)).ToArray();
            try
            {
                time.Advance(TimeSpan.FromSeconds(10));
                // Quatro perdedores da disputa precisam voltar a esperar (6 timers iniciais + 4 novos).
                await AsyncTest.WithTimeout(time.WaitForDelayCountAsync(10));
                await AsyncTest.WaitForCompletedCount(waiting, 2);
                Assert.Equal(2, waiting.Count(t => t.Status == TaskStatus.RanToCompletion));
                Assert.Equal(0, limiter.GetAvailableRequests("A"));

                time.Advance(TimeSpan.FromSeconds(10));
                await AsyncTest.WithTimeout(time.WaitForDelayCountAsync(12));
                await AsyncTest.WaitForCompletedCount(waiting, 4);
                Assert.Equal(4, waiting.Count(t => t.Status == TaskStatus.RanToCompletion));
                Assert.Equal(0, limiter.GetAvailableRequests("A"));

                time.Advance(TimeSpan.FromSeconds(10));
                await AsyncTest.WithTimeout(Task.WhenAll(waiting));
                Assert.Equal(0, limiter.GetAvailableRequests("A"));
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        [Fact]
        public async Task Providers_HaveIndependentCapacityAndResets()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromMinutes(1), time);
            using var cancellation = new CancellationTokenSource();
            await limiter.AcquireAsync("A");
            var waitingA = limiter.AcquireAsync("A", cancellation.Token);
            try
            {
                await AsyncTest.WithTimeout(limiter.AcquireAsync("B"));
                limiter.Reset("B");
                Assert.False(waitingA.IsCompleted);
                Assert.Equal(0, limiter.GetAvailableRequests("A"));
                Assert.Equal(1, limiter.GetAvailableRequests("B"));
            }
            finally
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingA);
            }
        }

        [Fact]
        public async Task CancellationWhileWaiting_DoesNotReserveAndDoesNotAffectOtherWaiters()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromSeconds(10), time);
            await limiter.AcquireAsync("A");
            using var cancellation = new CancellationTokenSource();
            var cancelled = limiter.AcquireAsync("A", cancellation.Token);
            var remaining = limiter.AcquireAsync("A");
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
            Assert.Equal(0, limiter.GetAvailableRequests("A"));
            Assert.False(remaining.IsCompleted);

            time.Advance(TimeSpan.FromSeconds(10));
            await AsyncTest.WithTimeout(remaining);
            time.Advance(TimeSpan.FromSeconds(10));
            Assert.Equal(1, limiter.GetAvailableRequests("A"));
        }

        [Fact]
        public async Task AlreadyCancelledToken_DoesNotConsumeAnAvailableSlot()
        {
            var limiter = new SlidingWindowRateLimiter(1);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => limiter.AcquireAsync("A", cancellation.Token));
            Assert.Equal(1, limiter.GetAvailableRequests("A"));
        }

        [Fact]
        public async Task Reset_WakesWaitingRequestsAndKeepsTheSameProtectedQueue()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromHours(1), time);
            using var cancellation = new CancellationTokenSource();
            await limiter.AcquireAsync("A");
            var waiting = Enumerable.Range(0, 8).Select(_ => limiter.AcquireAsync("A", cancellation.Token)).ToArray();
            try
            {
                limiter.Reset("A");
                await AsyncTest.WithTimeout(Task.WhenAny(waiting));
                await AsyncTest.WithTimeout(time.WaitForDelayCountAsync(15));
                Assert.Equal(1, waiting.Count(t => t.Status == TaskStatus.RanToCompletion));
                Assert.Equal(0, limiter.GetAvailableRequests("A"));
                Assert.Equal(TimeSpan.Zero, time.Elapsed);
            }
            finally
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(waiting));
            }
            Assert.Equal(0, time.ActiveDelays);
        }

        [Fact]
        public async Task ResetRecordReadAndAcquire_AreSafeUnderConcurrentAccess()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromMinutes(1), time);
            using var cancellation = new CancellationTokenSource();
            var pending = new ConcurrentBag<Task>();
            var workers = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
            {
                for (var i = 0; i < 300; i++)
                {
                    switch (worker % 4)
                    {
                        case 0: limiter.Reset("A"); break;
                        case 1: limiter.RecordRequest("A"); break;
                        case 2: Assert.InRange(limiter.GetAvailableRequests("A"), 0, 3); break;
                        case 3: pending.Add(limiter.AcquireAsync("A", cancellation.Token)); break;
                    }
                }
            })).ToArray();
            try
            {
                await AsyncTest.WithTimeout(Task.WhenAll(workers));
            }
            finally
            {
                cancellation.Cancel();
                await Task.WhenAll(pending.Select(async task =>
                {
                    try { await task; } catch (OperationCanceledException) { }
                }));
            }
            limiter.Reset("A");
            Assert.Equal(3, limiter.GetAvailableRequests("A"));
            await limiter.AcquireAsync("A");
            Assert.Equal(2, limiter.GetAvailableRequests("A"));
        }

        [Fact]
        public async Task LegacyWaitThenRecord_RegistersOnceAndRemainsCancellable()
        {
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromHours(1), new ManualRateLimiterTime());
            await limiter.WaitIfNeededAsync("A");
            Assert.Equal(1, limiter.GetAvailableRequests("A"));
            limiter.RecordRequest("A");
            Assert.Equal(0, limiter.GetAvailableRequests("A"));
            using var cancellation = new CancellationTokenSource();
            var waiting = limiter.WaitIfNeededAsync("A", cancellation.Token);
            Assert.False(waiting.IsCompleted);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            Assert.Equal(0, limiter.GetAvailableRequests("A"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Constructor_RejectsInvalidLimits(int limit)
        {
            Assert.Equal("maxRequests", Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindowRateLimiter(limit)).ParamName);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(long.MinValue)]
        public void Constructor_RejectsInvalidWindows(long ticks)
        {
            Assert.Equal("timeWindow", Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(1, TimeSpan.FromTicks(ticks))).ParamName);
        }

        [Fact]
        public async Task DefaultConstructor_UsesThreeRequestsAndOneMinute()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(3, null, time);
            Assert.Equal(3, new SlidingWindowRateLimiter().GetAvailableRequests("A"));
            for (var i = 0; i < 3; i++) await limiter.AcquireAsync("A");
            time.Advance(TimeSpan.FromSeconds(59));
            Assert.Equal(0, limiter.GetAvailableRequests("A"));
            time.Advance(TimeSpan.FromSeconds(1));
            Assert.Equal(3, limiter.GetAvailableRequests("A"));
        }

        [Fact]
        public async Task VeryLargeWindow_UsesBoundedCancellableDelays()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(int.MaxValue, TimeSpan.MaxValue, time);
            Assert.Equal(int.MaxValue, limiter.GetAvailableRequests("A"));
            limiter = new SlidingWindowRateLimiter(1, TimeSpan.MaxValue, time);
            await limiter.AcquireAsync("A");
            using var cancellation = new CancellationTokenSource();
            var waiting = limiter.AcquireAsync("A", cancellation.Token);
            Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue - 1), time.LastDelay);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        }

        [Fact]
        public async Task SubMillisecondWindow_DoesNotCreateAZeroDelayBusyLoop()
        {
            var time = new ManualRateLimiterTime();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromTicks(1), time);
            await limiter.AcquireAsync("A");
            var waiting = limiter.AcquireAsync("A");
            Assert.Equal(TimeSpan.FromMilliseconds(1), time.LastDelay);
            Assert.False(waiting.IsCompleted);
            time.Advance(TimeSpan.FromMilliseconds(1));
            await AsyncTest.WithTimeout(waiting);
        }
    }
}
