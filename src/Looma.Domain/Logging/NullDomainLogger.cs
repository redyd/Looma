// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Logging;

public sealed class NullDomainLogger : IDomainLogger
{
    public static NullDomainLogger Instance { get; } = new();

    private NullDomainLogger()
    {
    }

    public void Log(DomainLogLevel level, string message, Exception? exception = null)
    {
    }
}
