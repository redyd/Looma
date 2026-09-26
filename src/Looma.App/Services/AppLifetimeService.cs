// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Looma.Presentation.Services;

namespace Looma.App.Services;

public sealed class AppLifetimeService : IAppLifetimeService
{
    public void Restart()
    {
        var executable = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executable))
        {
            // Startup-only flags (--clear, --seed...) must not be replayed on restart.
            var args = Environment.GetCommandLineArgs()
                .Skip(1)
                .Where(arg => arg == "--local");
            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
            if (executable.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || executable.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            }

            foreach (var arg in args)
                startInfo.ArgumentList.Add(arg);

            Process.Start(startInfo);
        }

        Quit();
    }

    public void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }

    public void OpenFolder(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
