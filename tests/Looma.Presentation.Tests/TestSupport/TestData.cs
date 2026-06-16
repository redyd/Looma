// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Presentation.Tests.TestSupport;

internal static class TestData
{
    public static Wool Wool(
        int id = 1,
        string name = "Alpaca",
        string brand = "Drops",
        string material = "Alpaca",
        string color = "#336699",
        double weight = 50,
        double length = 167,
        double stock = 3000,
        double needleMin = 3,
        double needleMax = 4)
        => new()
        {
            Id = id,
            Name = name,
            Brand = brand,
            Material = material,
            Color = color,
            Weight = weight,
            Length = length,
            Stock = stock,
            NeedleMinSize = needleMin,
            NeedleMaxSize = needleMax
        };

    public static Document Document(
        Guid? id = null,
        string nickname = "Pattern PDF",
        string type = "pdf",
        long sizeBytes = 2048,
        string? storagePath = "/tmp/pattern.pdf",
        int? patternId = null,
        int? projectId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Nickname = nickname,
            Type = type,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            PatternId = patternId,
            ProjectId = projectId
        };

    public static Pattern Pattern(
        int id = 1,
        string name = "Cardigan",
        PatternType type = PatternType.Crochet,
        bool isPersonal = false,
        string? url = "https://example.test/pattern",
        string? note = "Warm",
        DateOnly? beginDate = null,
        DateOnly? endDate = null,
        IReadOnlyList<Document>? documents = null,
        IReadOnlyList<PatternProject>? projects = null)
        => new()
        {
            Id = id,
            Name = name,
            Type = type,
            IsPersonal = isPersonal,
            Url = url,
            Note = note,
            BeginDate = beginDate,
            EndDate = endDate,
            Documents = documents ?? [],
            Projects = projects ?? []
        };

    public static PatternProject PatternProject(
        int id = 1,
        string name = "Linked project",
        Status status = Status.InProgress)
        => new()
        {
            Id = id,
            Name = name,
            Status = status
        };

    public static WoolUsage WoolUsage(
        Wool? wool = null,
        double stockUsed = 1000,
        double stockAlreadyUsed = 0)
        => new()
        {
            Wool = wool ?? Wool(),
            StockUsed = stockUsed,
            StockAlreadyUsed = stockAlreadyUsed
        };

    public static Project Project(
        int id = 1,
        string name = "Sweater",
        Status status = Status.InProgress,
        Pattern? pattern = null,
        string? note = "Project note",
        DateOnly? beginDate = null,
        DateOnly? endDate = null,
        IReadOnlyList<WoolUsage>? wools = null,
        IReadOnlyList<Document>? files = null)
        => new()
        {
            ProjectId = id,
            Name = name,
            Status = status,
            Pattern = pattern,
            Note = note,
            BeginDate = beginDate,
            EndDate = endDate,
            Wools = wools ?? [],
            Files = files ?? []
        };
}
