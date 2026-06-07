// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.Services;

public sealed class DataRefreshService : IDataRefreshService
{
    public event EventHandler? DocumentsRefreshRequested;
    public event EventHandler? PatternsRefreshRequested;

    public void RequestDocumentsRefresh() => DocumentsRefreshRequested?.Invoke(this, EventArgs.Empty);

    public void RequestPatternsRefresh() => PatternsRefreshRequested?.Invoke(this, EventArgs.Empty);
}
