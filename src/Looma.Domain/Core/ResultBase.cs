// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Core;

public abstract class ResultBase(ResultStatus status, string? error = null)
{
    public string? Error { get; } = error;
    public ResultStatus Status { get; } = status;
    public bool Failed => Status != ResultStatus.Success;
    public bool Succeeded => Status == ResultStatus.Success;
}