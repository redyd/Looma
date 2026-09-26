// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Repositories;

public interface IUnitOfWork
{
    /// <summary>
    /// Runs several repository operations atomically: every change is committed when the
    /// returned result succeeded, and everything is rolled back when it failed or threw.
    /// Nested calls join the outer unit of work.
    /// </summary>
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action) where TResult : ResultBase;
}
