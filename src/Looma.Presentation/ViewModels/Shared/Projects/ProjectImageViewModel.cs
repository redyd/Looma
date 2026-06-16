// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Shared.Projects;

public record ProjectImageViewModel(
    Document Document,
    ICommand? RemoveCommand = null)
{
    public string Name => Document.Nickname;
    public string? SourcePath => Document.StoragePath;
    public string DetailText => $"{Document.Type} · {Document.SizeBytes.ToBytesDisplay()}";
}
