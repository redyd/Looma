// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Request;

namespace Looma.Domain.IServices;

public interface IWoolStockService
{
    Task<Result> AdjustWoolUsageAsync(AdjustProjectWoolUsageRequest request);
}
