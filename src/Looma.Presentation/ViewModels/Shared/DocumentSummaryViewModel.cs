// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared;

public record DocumentSummaryViewModel(
    Document Document,
    ICommand OpenCommand,
    ICommand? OpenOriginCommand = null,
    ICommand? EditCommand = null,
    ICommand? DeleteCommand = null)
{
    public string TypeDisplay => Document.Type;
    public string SizeDisplay => Document.SizeBytes.ToBytesDisplay();
    public bool HasOrigin => Document.PatternId.HasValue || Document.ProjectId.HasValue;

    public string OriginTypeDisplay =>
        Document.PatternId.HasValue ? TranslationService.Current["Common_Pattern"] :
        Document.ProjectId.HasValue ? TranslationService.Current["Projects_Title"] :
        TranslationService.Current["Common_NoneFeminine"];
}
