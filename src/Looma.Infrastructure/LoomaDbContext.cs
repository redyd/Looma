// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Configurations;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Looma.Domain.Localization;

namespace Looma.Infrastructure;

public class LoomaDbContext(DbContextOptions<LoomaDbContext> options) : DbContext(options)
{
    public DbSet<WoolEntity> Wools => Set<WoolEntity>();
    public DbSet<PatternEntity> Patterns => Set<PatternEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<WoolsForProjectEntity> WoolsForProjects => Set<WoolsForProjectEntity>();
    public DbSet<TrackedWool> TrackedWools => Set<TrackedWool>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new WoolForProjectConfiguration())
            .ApplyConfiguration(new ProjectConfiguration())
            .ApplyConfiguration(new DocumentEntityConfiguration())
            .ApplyConfiguration(new WoolConfiguration())
            .ApplyConfiguration(new PatternConfiguration())
            .ApplyConfiguration(new TrackedWoolConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureValidNumbers();
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch
        {
            DiscardPendingChangesOutsideTransaction();
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureValidNumbers();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            DiscardPendingChangesOutsideTransaction();
            throw;
        }
    }

    /// <summary>
    /// The context lives for the whole session: a failed save must not leave its changes
    /// pending, otherwise every later save would retry (and fail on) them.
    /// Inside a transaction the unit of work owns the cleanup.
    /// </summary>
    private void DiscardPendingChangesOutsideTransaction()
    {
        if (Database.CurrentTransaction is null)
            ChangeTracker.Clear();
    }

    private void EnsureValidNumbers()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is double value && !double.IsFinite(value))
                {
                    DiscardPendingChangesOutsideTransaction();
                    throw new InvalidOperationException(Localizer.Format(
                        "Data_Errors_InvalidNumericValue", entry.Metadata.ClrType.Name, property.Metadata.Name));
                }
            }
        }
    }
}
