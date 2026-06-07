// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Configurations;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure;

public class LoomaDbContext(DbContextOptions<LoomaDbContext> options) : DbContext(options)
{
    public DbSet<WoolEntity> Wools => Set<WoolEntity>();
    public DbSet<PatternEntity> Patterns => Set<PatternEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<WoolsForProjectEntity> WoolsForProjects => Set<WoolsForProjectEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new WoolForProjectConfiguration())
            .ApplyConfiguration(new ProjectConfiguration())
            .ApplyConfiguration(new DocumentConfiguration())
            .ApplyConfiguration(new WoolConfiguration())
            .ApplyConfiguration(new PatternConfiguration());
    }
}