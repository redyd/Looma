// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Logging;

namespace Looma.Domain.Services;

public abstract class DomainServiceBase(IDomainLogger? logger)
{
    protected IDomainLogger Logger { get; } = logger ?? NullDomainLogger.Instance;

    protected async Task<Result> ExecuteAsync(string operation, Func<Task<Result>> action)
    {
        Logger.Log(DomainLogLevel.Information, $"{operation} started.");

        try
        {
            var result = await action();
            LogResult(operation, result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Log(DomainLogLevel.Error, $"{operation} failed with an exception.", ex);
            return Result.Failure(ex.Message);
        }
    }

    protected async Task<ResultT<T>> ExecuteAsync<T>(string operation, Func<Task<ResultT<T>>> action)
    {
        Logger.Log(DomainLogLevel.Information, $"{operation} started.");

        try
        {
            var result = await action();
            LogResult(operation, result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Log(DomainLogLevel.Error, $"{operation} failed with an exception.", ex);
            return ResultT<T>.Failure(ex.Message);
        }
    }

    protected void LogResult(string operation, ResultBase result)
    {
        if (result.Failed)
        {
            Logger.Log(DomainLogLevel.Warning, $"{operation} failed: {result.Error ?? result.Status.ToString()}.");
            return;
        }

        Logger.Log(DomainLogLevel.Information, $"{operation} completed.");
    }
}
