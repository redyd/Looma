// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using Looma.Domain.Logging;

namespace Looma.App.Services;

public sealed class ConsoleDomainLogger : IDomainLogger
{
    public void Log(DomainLogLevel level, string message, Exception? exception = null)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] {message}";
        if (level == DomainLogLevel.Error)
        {
            Console.Error.WriteLine(line);
            if (exception is not null)
            {
                Console.Error.WriteLine(exception);
            }

            return;
        }

        Console.WriteLine(line);
    }
}
