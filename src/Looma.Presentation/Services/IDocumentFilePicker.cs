// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;

namespace Looma.Presentation.Services;

public interface IDocumentFilePicker
{
    Task<string?> PickAsync(DocumentPickerMode mode);
    Task<List<string>> PicksAsync(DocumentPickerMode mode);
    bool IsSupportedFile(DocumentPickerMode mode, Document document);
}
