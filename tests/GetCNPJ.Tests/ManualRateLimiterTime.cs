using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.RateLimiter;

namespace GetCNPJ.Tests
{
    internal sealed class ManualRateLimiterTime : IRateLimiterTime
    {
        private readonly object _sync = new object();
        private readonly List<ScheduledDelay> _delays = new List<ScheduledDelay>();
        private TimeSpan _elapsed;
        private int _delayCount;
        private TaskCompletionSource<bool> _delayChanged = NewSignal();

        private sealed class ScheduledDelay
        {
            public TimeSpan Due;
            public readonly TaskCompletionSource<bool> Completion = NewSignal();
        }

        public TimeSpan Elapsed { get { lock (_sync) return _elapsed; } }
        public TimeSpan LastDelay { get; private set; }
        public int ActiveDelays { get { lock (_sync) return _delays.Count(d => !d.Completion.Task.IsCompleted); } }

        public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduledDelay scheduled;
            TaskCompletionSource<bool> changed;
            lock (_sync)
            {
                scheduled = new ScheduledDelay { Due = _elapsed + delay };
                _delays.Add(scheduled);
                LastDelay = delay;
                _delayCount++;
                changed = _delayChanged;
                _delayChanged = NewSignal();
            }
            changed.TrySetResult(true);
            try
            {
                using (cancellationToken.Register(() => scheduled.Completion.TrySetCanceled()))
                    await scheduled.Completion.Task.ConfigureAwait(false);
            }
            finally
            {
                lock (_sync) _delays.Remove(scheduled);
            }
        }

        public void Advance(TimeSpan amount)
        {
            if (amount < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(amount));
            ScheduledDelay[] due;
            lock (_sync)
            {
                _elapsed += amount;
                due = _delays.Where(d => d.Due <= _elapsed).ToArray();
            }
            foreach (var delay in due) delay.Completion.TrySetResult(true);
        }

        public async Task WaitForDelayCountAsync(int count)
        {
            while (true)
            {
                Task changed;
                lock (_sync)
                {
                    if (_delayCount >= count) return;
                    changed = _delayChanged.Task;
                }
                await changed.ConfigureAwait(false);
            }
        }

        private static TaskCompletionSource<bool> NewSignal()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
