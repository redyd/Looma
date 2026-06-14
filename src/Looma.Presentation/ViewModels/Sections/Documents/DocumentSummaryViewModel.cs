// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public record DocumentSummaryViewModel(
    Document Document,
    ICommand OpenCommand,
    ICommand OpenOriginCommand,
    ICommand EditCommand,
    ICommand DeleteCommand)
{

    public string TypeDisplay => Document.Type;
    public string SizeDisplay => Document.SizeBytes.ToBytesDisplay();
    public bool HasOrigin => Document.PatternId.HasValue || Document.ProjectId.HasValue;
    public string OriginTypeDisplay =>
        Document.PatternId.HasValue ? "Patron" :
        Document.ProjectId.HasValue ? "Projet" :
        "Aucune";

}
