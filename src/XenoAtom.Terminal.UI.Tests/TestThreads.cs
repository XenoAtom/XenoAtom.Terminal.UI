// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.ExceptionServices;

namespace XenoAtom.Terminal.UI.Tests;

/// <summary>
/// Helpers to run test code on dedicated threads (not thread-pool threads, which <c>Task.Wait</c> may inline).
/// </summary>
internal static class TestThreads
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Runs <paramref name="action"/> on a new dedicated thread, waits for it, and rethrows its exception (including assertion failures).
    /// </summary>
    public static void Run(Action action, string name = "TestThread")
    {
        ArgumentNullException.ThrowIfNull(action);

        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        })
        {
            IsBackground = true,
            Name = name,
        };

        thread.Start();
        if (!thread.Join(Timeout))
        {
            throw new TimeoutException($"The thread `{name}` did not complete in time.");
        }

        failure?.Throw();
    }

    /// <summary>
    /// Runs <paramref name="action"/> concurrently on <paramref name="count"/> dedicated threads and rethrows the first failure.
    /// </summary>
    public static void RunConcurrently(int count, Action<int> action, string name = "TestThread")
    {
        ArgumentNullException.ThrowIfNull(action);

        var failures = new ExceptionDispatchInfo?[count];
        var threads = new Thread[count];
        for (var i = 0; i < count; i++)
        {
            var index = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    action(index);
                }
                catch (Exception ex)
                {
                    failures[index] = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                IsBackground = true,
                Name = $"{name}-{index}",
            };
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            if (!thread.Join(Timeout))
            {
                throw new TimeoutException($"The thread `{thread.Name}` did not complete in time.");
            }
        }

        foreach (var failure in failures)
        {
            failure?.Throw();
        }
    }
}
