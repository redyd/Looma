// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.Services;

public interface IDataRefreshService
{
    event EventHandler? DocumentsRefreshRequested;
    event EventHandler? PatternsRefreshRequested;

    void RequestDocumentsRefresh();
    void RequestPatternsRefresh();
}
