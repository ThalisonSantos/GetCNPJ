using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GetCNPJ.Tests
{
    internal static class AsyncTest
    {
        public static async Task WaitForCompletedCount(IReadOnlyCollection<Task> tasks, int count)
        {
            while (tasks.Count(t => t.IsCompleted) < count)
                await WithTimeout(Task.WhenAny(tasks.Where(t => !t.IsCompleted)));
        }

        // Apenas proteção contra travamento; as regras temporais são verificadas com tempo manual.
        public static async Task WithTimeout(Task task)
        {
            using var timeout = new CancellationTokenSource();
            var watchdog = Task.Delay(TimeSpan.FromSeconds(15), timeout.Token);
            try
            {
                Assert.Same(task, await Task.WhenAny(task, watchdog));
                await task;
            }
            finally
            {
                timeout.Cancel();
            }
        }
    }
}
