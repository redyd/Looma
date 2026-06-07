// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Looma.Infrastructure;

public class LoomaDbContextFactory : IDesignTimeDbContextFactory<LoomaDbContext>
{
    public LoomaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite("Data Source=looma.db")
            .Options;

        return new LoomaDbContext(options);
    }
}