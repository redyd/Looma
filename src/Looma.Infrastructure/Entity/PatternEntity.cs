// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Infrastructure.Entity;

public class PatternEntity
{
    public int PatternId { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public string? Note { get; set; }
    public DateOnly? BeginDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public PatternType Type { get; set; }
    public bool IsPersonal { get; set; }
    
    // RELATIONS
    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    public ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
}
