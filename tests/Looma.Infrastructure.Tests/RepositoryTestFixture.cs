// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure;
using Looma.Infrastructure.Entity;
using Looma.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests;

internal sealed class RepositoryTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public RepositoryTestFixture()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "looma-infra-tests", Guid.NewGuid().ToString("N"));
        Paths = new AppPaths(RootPath);
        Paths.EnsureDirectoriesExist();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public string RootPath { get; }
    public AppPaths Paths { get; }
    public DbContextOptions<LoomaDbContext> Options { get; }

    public LoomaDbContext CreateContext() => new(Options);

    public string CreateSourceFile(string fileName, string content = "document content")
    {
        var sourcePath = Path.Combine(RootPath, "sources", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, content);
        return sourcePath;
    }

    public async Task<PatternEntity> AddPatternAsync(
        string name = "Pattern",
        PatternType type = PatternType.Crochet,
        bool isPersonal = false)
    {
        await using var context = CreateContext();
        var entity = new PatternEntity
        {
            Name = name,
            Type = type,
            IsPersonal = isPersonal
        };

        context.Patterns.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<WoolEntity> AddWoolAsync(
        string name = "Wool",
        string brand = "Brand",
        double stock = 1000)
    {
        await using var context = CreateContext();
        var entity = new WoolEntity
        {
            Name = name,
            Brand = brand,
            Material = "Merino",
            Color = "Blue",
            Weight = 50,
            Length = 120,
            Stock = stock,
            NeedleMinSize = 3,
            NeedleMaxSize = 4
        };

        context.Wools.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<ProjectEntity> AddProjectAsync(
        int? patternId,
        IEnumerable<int>? woolIds = null,
        string name = "Project")
    {
        await using var context = CreateContext();
        var entity = new ProjectEntity
        {
            Name = name,
            Status = Status.InProgress,
            PatternId = patternId,
            WoolsForProjects = woolIds?
                .Select(id => new WoolsForProjectEntity { WoolId = id, StockUsed = 0, StockAlreadyUsed = 0 })
                .ToList() ?? []
        };

        context.Projects.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<DocumentEntity> AddDocumentEntityAsync(
        string nickname = "Document",
        int? patternId = null,
        int? projectId = null,
        string? type = "PDF",
        long? size = 12)
    {
        await using var context = CreateContext();
        var entity = new DocumentEntity
        {
            DocumentId = Guid.NewGuid(),
            Nickname = nickname,
            Type = type,
            Size = size,
            PatternId = patternId,
            ProjectId = projectId
        };

        context.Documents.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public UpdateWoolRequest ValidUpdateWoolRequest(int id, string name = "Updated wool") =>
        new(id, name, "Updated brand", "Cotton", ["Red"], 100, 240, 4, 5);

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
