// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Infrastructure.Entity;

public class WoolEntity
{
    public int WoolId { get; set; }
    public string Name { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Material { get; set; } = null!;
    public string Color { get; set; } = null!;
    public double Weight { get; set; }
    public double Length { get; set; }
    // le stock représente une unité de pelotte multiplié par 1000 pour plus de précisions (1 pelotte = 1000, 3.21 pelottes = 3210)
    public double Stock { get; set; }
    public double NeedleMinSize { get; set; }
    public double NeedleMaxSize { get; set; }

    public ICollection<WoolsForProjectEntity> WoolsForProjects { get; set; } = new List<WoolsForProjectEntity>();
    public ICollection<TrackedWool> TrackedWools { get; set; } = [];
}
