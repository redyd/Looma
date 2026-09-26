// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Repositories;

namespace Looma.Infrastructure.Repositories;

public sealed class UnitOfWork(LoomaDbContext context) : IUnitOfWork
{
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action) where TResult : ResultBase
    {
        if (context.Database.CurrentTransaction is not null)
            return await action();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
                return result;
            }

            await RollbackAsync(transaction);
            return result;
        }
        catch
        {
            await RollbackAsync(transaction);
            throw;
        }
    }

    private async Task RollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        await transaction.RollbackAsync();
        // Tracked entities still hold the rolled-back values: drop them so the next reads hit the database.
        context.ChangeTracker.Clear();
    }
}
