// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Infrastructure.Entity;

public class ProjectEntity
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly? BeginDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int PatronId { get; set; }
    public PatternEntity PatternEntity { get; set; } = null!;

    public Status Status { get; set; }

    public ICollection<WoolsForProjectEntity> WoolsForProjects { get; set; } = new List<WoolsForProjectEntity>();
}