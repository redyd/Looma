// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Infrastructure.Entity;

public class DocumentEntity
{
    public Guid DocumentId { get; set; }
    public string Nickname { get; set; } = null!;
    public PatternEntity Pattern = null!;
}
