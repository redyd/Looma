// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Request;
using Looma.Domain.Services;

namespace Looma.App.Services;

public sealed class AppDataSeeder(
    IWoolService woolService,
    IPatternService patternService,
    IProjectService projectService,
    IDocumentService documentService) : IAppDataSeeder
{
    public async Task SeedAsync(int? itemCount = null)
    {
        await EnsureDatabaseIsEmptyAsync();

        var wools = await SeedWoolsAsync(itemCount);
        var patterns = await SeedPatternsAsync(itemCount);
        await SeedProjectsAsync(wools, patterns, itemCount);
    }

    private async Task EnsureDatabaseIsEmptyAsync()
    {
        var wools = await woolService.GetAllAsync();
        EnsureSucceeded(wools, "verifier les laines existantes");

        var patterns = await patternService.GetAllAsync();
        EnsureSucceeded(patterns, "verifier les patrons existants");

        var projects = await projectService.GetAllAsync();
        EnsureSucceeded(projects, "verifier les projets existants");

        var documents = await documentService.GetAllAsync();
        EnsureSucceeded(documents, "verifier les documents existants");

        if ((wools.Value?.Count ?? 0) > 0
            || (patterns.Value?.Count ?? 0) > 0
            || (projects.Value?.Count ?? 0) > 0
            || (documents.Value?.Count ?? 0) > 0)
        {
            throw new InvalidOperationException(
                "Le seed ne peut s'executer que sur une base vide. Lancez l'application avec --clear --seed pour regenerer les donnees de demonstration.");
        }
    }

    private async Task<IReadOnlyList<Wool>> SeedWoolsAsync(int? itemCount)
    {
        var existing = await woolService.GetAllAsync();
        EnsureSucceeded(existing, "charger les laines existantes");

        var wools = existing.Value?.ToList() ?? [];
        foreach (var request in WoolRequests(itemCount))
        {
            var existingWool = wools.FirstOrDefault(w =>
                string.Equals(w.Name, request.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(w.Brand, request.Brand, StringComparison.OrdinalIgnoreCase));

            if (existingWool is not null)
                continue;

            var added = await woolService.AddAsync(request);
            EnsureSucceeded(added, $"ajouter la laine {request.Brand} {request.Name}");
            wools.Add(added.Value!);
        }

        return wools;
    }

    private async Task<IReadOnlyList<Pattern>> SeedPatternsAsync(int? itemCount)
    {
        var existing = await patternService.GetAllAsync();
        EnsureSucceeded(existing, "charger les patrons existants");

        var patterns = existing.Value?.ToList() ?? [];
        foreach (var request in PatternRequests(itemCount))
        {
            var pattern = patterns.FirstOrDefault(p =>
                string.Equals(p.Name, request.Name, StringComparison.OrdinalIgnoreCase));

            if (pattern is null)
            {
                var added = await patternService.AddAsync(request);
                EnsureSucceeded(added, $"ajouter le patron {request.Name}");
                pattern = added.Value!;
                patterns.Add(pattern);
            }

            if (pattern.Documents.Count == 0)
            {
                await AddPatternDocumentsAsync(pattern, itemCount.HasValue ? 1 : 2);
            }
        }

        return patterns;
    }

    private async Task SeedProjectsAsync(IReadOnlyList<Wool> wools, IReadOnlyList<Pattern> patterns, int? itemCount)
    {
        var existing = await projectService.GetAllAsync();
        EnsureSucceeded(existing, "charger les projets existants");

        var existingNames = (existing.Value ?? [])
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var statuses = Enum.GetValues<Status>();
        var projectCount = itemCount ?? statuses.Length;
        for (var i = 0; i < projectCount; i++)
        {
            var status = statuses[i % statuses.Length];
            var name = itemCount.HasValue
                ? $"Seed - Projet {i + 1:000} - {GetStatusLabel(status)}"
                : $"Seed - Projet {GetStatusLabel(status)}";
            if (existingNames.Contains(name))
                continue;

            int? patternId = patterns.Count == 0 ? null : patterns[i % patterns.Count].Id;
            var woolIds = wools
                .Skip(i * 2)
                .Take(3)
                .Select(w => w.Id)
                .ToList();

            if (woolIds.Count == 0)
                woolIds = [.. wools.Take(3).Select(w => w.Id)];

            var added = await projectService.AddAsync(new CreateProjectRequest(
                name,
                status,
                $"Projet de demonstration pour le statut {GetStatusLabel(status)}.",
                new DateOnly(2026, 1, 5).AddMonths(i),
                status == Status.Finished ? new DateOnly(2026, 2, 15).AddMonths(i) : null,
                patternId,
                woolIds));

            EnsureSucceeded(added, $"ajouter le projet {name}");
        }
    }

    private async Task AddPatternDocumentsAsync(Pattern pattern, int documentCount)
    {
        var sourcePaths = CreateSeedDocumentSources(pattern, documentCount);

        try
        {
            var added = await documentService.AddAllAsync(
                sourcePaths
                    .Select((path, index) => new CreateDocumentRequest(
                        path,
                        index == 0 ? $"{pattern.Name} - Instructions" : $"{pattern.Name} - Notes {index + 1}",
                        PatternId: pattern.Id))
                    .ToList());

            EnsureSucceeded(added, $"ajouter les documents du patron {pattern.Name}");
        }
        finally
        {
            foreach (var sourcePath in sourcePaths)
            {
                if (File.Exists(sourcePath))
                    File.Delete(sourcePath);
            }

            var directory = Path.GetDirectoryName(sourcePaths[0]);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static IReadOnlyList<string> CreateSeedDocumentSources(Pattern pattern, int documentCount = 2)
    {
        var directory = Path.Combine(Path.GetTempPath(), "looma-seed", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var paths = new List<string>(documentCount);
        for (var i = 0; i < documentCount; i++)
        {
            var suffix = i == 0 ? "instructions" : $"notes-{i + 1}";
            var extension = i == 0 ? "pdf" : "txt";
            var path = Path.Combine(directory, $"{SanitizeFileName(pattern.Name)}-{suffix}.{extension}");
            File.WriteAllText(path, $"Document de demonstration {i + 1} pour {pattern.Name}.");
            paths.Add(path);
        }

        return paths;
    }

    private static IEnumerable<CreatePatternRequest> PatternRequests(int? itemCount)
    {
        var templates = new[]
        {
            new CreatePatternRequest(
            "Seed - Chale crochet",
            "https://example.com/chale-crochet",
            "Patron de demonstration crochet avec documents.",
            PatternType.Crochet,
            false,
            new DateOnly(2026, 1, 10)),

            new CreatePatternRequest(
            "Seed - Echarpe tunisienne",
            null,
            "Patron personnel de demonstration en crochet tunisien.",
            PatternType.TunisianCrochet,
            true,
            new DateOnly(2026, 2, 1)),

            new CreatePatternRequest(
            "Seed - Pull tricot",
            "https://example.com/pull-tricot",
            "Patron de demonstration tricot avec documents.",
            PatternType.Tricot,
            false,
            new DateOnly(2026, 3, 5))
        };

        if (!itemCount.HasValue)
            return templates;

        return Enumerable.Range(0, itemCount.Value)
            .Select(i =>
            {
                var template = templates[i % templates.Length];
                return template with
                {
                    Name = $"{template.Name} {i + 1:000}",
                    Url = template.Url is null ? null : $"{template.Url}-{i + 1:000}",
                    BeginDate = template.BeginDate?.AddDays(i)
                };
            });
    }

    private static IEnumerable<CreateWoolRequest> WoolRequests(int? itemCount)
    {
        var templates = new[]
        {
            new CreateWoolRequest("Lace Cloud", "Seed Yarn Co", "Alpaga", ["#F7E7CE"], 50, 420, 4000, 1.0, 2.0),
            new CreateWoolRequest("Sock Twist", "Seed Yarn Co", "Merinos nylon", ["#2E86AB", "#F6F5AE"], 50, 210, 6000, 2.25, 3.0),
            new CreateWoolRequest("Fine Merino", "Atelier Demo", "Merinos", ["#D7263D"], 50, 175, 5000, 3.25, 3.75),
            new CreateWoolRequest("Light Cotton", "Atelier Demo", "Coton", ["#1B998B"], 100, 250, 3000, 4.0, 4.75),
            new CreateWoolRequest("Everyday DK", "Maille Test", "Laine", ["#F46036"], 100, 220, 4500, 5.0, 5.75),
            new CreateWoolRequest("Medium Wool", "Maille Test", "Laine vierge", ["#2D3047"], 100, 180, 3500, 5.0, 5.75),
            new CreateWoolRequest("Bulky Tweed", "Pelote Seed", "Laine tweed", ["#8D99AE", "#EDF2F4"], 100, 120, 2500, 6.0, 8.25),
            new CreateWoolRequest("Super Bulky", "Pelote Seed", "Acrylique laine", ["#FFB703"], 150, 90, 2000, 8.5, 13.75),
            new CreateWoolRequest("Jumbo Roving", "Chunky Demo", "Laine meche", ["#6A4C93"], 200, 60, 1500, 14.0, double.MaxValue),
            new CreateWoolRequest("Gradient Cotton", "Chunky Demo", "Coton recycle", ["#06D6A0", "#118AB2", "#073B4C"], 100, 300, 3200, 4.0, 4.75)
        };

        if (!itemCount.HasValue)
            return templates;

        return Enumerable.Range(0, itemCount.Value)
            .Select(i =>
            {
                var template = templates[i % templates.Length];
                return template with
                {
                    Name = $"{template.Name} {i + 1:000}",
                    Stock = template.Stock + (i % 12) * template.Weight
                };
            });
    }

    private static string GetStatusLabel(Status status) => status switch
    {
        Status.Wishlist => "wishlist",
        Status.InProgress => "en cours",
        Status.Finished => "termine",
        Status.Paused => "en pause",
        _ => status.ToString()
    };

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalidChars.Contains(c) ? '-' : c).ToArray());
    }

    private static void EnsureSucceeded(ResultBase result, string action)
    {
        if (result.Failed)
            throw new InvalidOperationException($"Impossible de {action}: {result.Error}");
    }
}
