// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Core;

public class Result : ResultBase
{
    private Result(ResultStatus status = ResultStatus.Success, string? error = null)
        : base(status, error)
    {
    }

    public static Result Ok() => new();

    public static Result NotFound(string error) =>
        new(ResultStatus.NotFound, error);

    public static Result Forbidden(string error) =>
        new(ResultStatus.Forbidden, error);

    public static Result Conflict(string error) =>
        new(ResultStatus.Conflict, error);

    public static Result Failure(string error) =>
        new(ResultStatus.Failure, error);
}