// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Looma.Presentation.Tests.TestSupport;

internal static class TestHelpers
{
    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 1000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        while (!condition())
        {
            if (cts.IsCancellationRequested)
                throw new TimeoutException("Condition was not met before the timeout.");

            await Task.Delay(10);
        }
    }

    public static Task ExecuteAsync(this IAsyncRelayCommand command) =>
        command.ExecuteAsync(null);

    public static Task ExecuteAsync(this ICommand command)
    {
        if (command is IAsyncRelayCommand asyncCommand)
            return asyncCommand.ExecuteAsync(null);

        command.Execute(null);
        return Task.CompletedTask;
    }
}
