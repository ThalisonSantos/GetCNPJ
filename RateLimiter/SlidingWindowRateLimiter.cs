using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GetCNPJ.Interfaces;

namespace GetCNPJ.RateLimiter
{
    /// <summary>
    /// Implementação de Rate Limiter usando Sliding Window
    /// Limita o número de requisições por intervalo de tempo
    /// </summary>
    public class SlidingWindowRateLimiter : IAtomicRateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly IRateLimiterTime _time;
        private readonly ConcurrentDictionary<string, ProviderWindow> _windows;

        private sealed class ProviderWindow
        {
            public readonly object Sync = new object();
            public readonly Queue<TimeSpan> Timestamps = new Queue<TimeSpan>();
            public TaskCompletionSource<bool> ResetSignal = CreateResetSignal();
        }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="maxRequests">Número máximo de requisições no intervalo</param>
        /// <param name="timeWindow">Intervalo de tempo (padrão: 1 minuto)</param>
        public SlidingWindowRateLimiter(int maxRequests = 3, TimeSpan? timeWindow = null)
            : this(maxRequests, timeWindow, new StopwatchRateLimiterTime())
        {
        }

        internal SlidingWindowRateLimiter(int maxRequests, TimeSpan? timeWindow, IRateLimiterTime time)
        {
            if (maxRequests <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRequests), "O limite deve ser maior que zero");

            var window = timeWindow ?? TimeSpan.FromMinutes(1);
            if (window <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeWindow), "O intervalo deve ser maior que zero");

            _maxRequests = maxRequests;
            _timeWindow = window;
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _windows = new ConcurrentDictionary<string, ProviderWindow>();
        }

        /// <inheritdoc/>
        public Task AcquireAsync(string providerName, CancellationToken cancellationToken = default)
        {
            return WaitForSlotAsync(providerName, reserve: true, cancellationToken);
        }

        /// <inheritdoc/>
        public Task WaitIfNeededAsync(string providerName, CancellationToken cancellationToken = default)
        {
            // Mantém o comportamento do contrato legado: esperar não registra uma requisição.
            return WaitForSlotAsync(providerName, reserve: false, cancellationToken);
        }

        private async Task WaitForSlotAsync(string providerName, bool reserve, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var window = _windows.GetOrAdd(providerName, _ => new ProviderWindow());

            while (true)
            {
                TimeSpan waitTime;
                Task resetSignal;
                lock (window.Sync)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var now = _time.Elapsed;
                    CleanOldTimestamps(window.Timestamps, now);
                    if (window.Timestamps.Count < _maxRequests)
                    {
                        if (reserve)
                            window.Timestamps.Enqueue(now);
                        return;
                    }

                    waitTime = _timeWindow - (now - window.Timestamps.Peek());
                    resetSignal = window.ResetSignal.Task;
                }

                // Task.Delay do .NET Framework limita milissegundos a Int32.MaxValue.
                // Arredondar para cima evita espera zero em janelas menores que 1 ms.
                var milliseconds = (int)Math.Min(int.MaxValue - 1L,
                    (waitTime.Ticks - 1) / TimeSpan.TicksPerMillisecond + 1);
                using (var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    var delay = _time.DelayAsync(TimeSpan.FromMilliseconds(milliseconds), delayCancellation.Token);
                    try
                    {
                        if (await Task.WhenAny(delay, resetSignal).ConfigureAwait(false) == delay)
                            await delay.ConfigureAwait(false);
                    }
                    finally
                    {
                        // Reset pode vencer a corrida: não deixar o timer antigo ativo.
                        delayCancellation.Cancel();
                    }
                }
                // Sempre reavaliar a fila e o cancelamento, inclusive após Reset.
            }
        }

        /// <inheritdoc/>
        public void RecordRequest(string providerName)
        {
            var window = _windows.GetOrAdd(providerName, _ => new ProviderWindow());
            lock (window.Sync)
            {
                var now = _time.Elapsed;
                CleanOldTimestamps(window.Timestamps, now);
                window.Timestamps.Enqueue(now);
            }
        }

        /// <inheritdoc/>
        public void Reset(string providerName)
        {
            var window = _windows.GetOrAdd(providerName, _ => new ProviderWindow());
            TaskCompletionSource<bool> signal;
            lock (window.Sync)
            {
                // Não remover o estado: chamadas em andamento precisam usar a mesma fila/lock.
                window.Timestamps.Clear();
                signal = window.ResetSignal;
                window.ResetSignal = CreateResetSignal();
            }
            signal.TrySetResult(true);
        }

        /// <inheritdoc/>
        public int GetAvailableRequests(string providerName)
        {
            if (!_windows.TryGetValue(providerName, out var window))
                return _maxRequests;

            lock (window.Sync)
            {
                CleanOldTimestamps(window.Timestamps, _time.Elapsed);
                return Math.Max(0, _maxRequests - window.Timestamps.Count);
            }
        }

        /// <summary>
        /// Remove timestamps fora da janela de tempo
        /// </summary>
        private void CleanOldTimestamps(Queue<TimeSpan> timestamps, TimeSpan now)
        {
            while (timestamps.Count > 0 && now - timestamps.Peek() >= _timeWindow)
            {
                timestamps.Dequeue();
            }
        }

        private static TaskCompletionSource<bool> CreateResetSignal()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
