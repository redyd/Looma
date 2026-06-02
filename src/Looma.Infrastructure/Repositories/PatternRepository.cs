using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Mapping;
using Looma.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class PatternRepository(LoomaDbContext context) : IPatternRepository
{
    public async Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync()
    {
        try
        {
            var entities = await context.Patterns
                .AsNoTracking()
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return ResultT<IReadOnlyList<Pattern>>.Ok(entities.Select(e => e.ToDomain()).ToList());
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Pattern>>.Failure($"Impossible de charger les patrons: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await context.Patterns
                .AsNoTracking()
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.PatternId == id);

            return entity is null
                ? ResultT<Pattern>.NotFound($"Le patron {id} est introuvable.")
                : ResultT<Pattern>.Ok(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de charger le patron {id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure("Le nom du patron est invalide.");

            var documents = await ResolveDocumentsAsync(request.DocumentIds);
            if (documents.Failed)
                return ResultT<Pattern>.Failure(documents.Error ?? "Impossible de résoudre les documents du patron.");

            var entity = new PatternEntity
            {
                Name = name,
                Url = NormalizeOptional(request.Url),
                Note = NormalizeOptional(request.Note),
                Documents = documents.Value!.ToList(),
                Projects = []
            };

            context.Patterns.Add(entity);
            await context.SaveChangesAsync();

            return ResultT<Pattern>.Ok((await context.Patterns.AsNoTracking()
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstAsync(p => p.PatternId == entity.PatternId)).ToDomain());
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Pattern>.Failure($"Impossible d'ajouter le patron: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible d'ajouter le patron: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request)
    {
        try
        {
            var entity = await context.Patterns
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.PatternId == request.Id);
            if (entity is null)
                return ResultT<Pattern>.NotFound($"Le patron {request.Id} est introuvable.");

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure("Le nom du patron est invalide.");

            var documents = await ResolveDocumentsAsync(request.DocumentIds);
            if (documents.Failed)
                return ResultT<Pattern>.Failure(documents.Error ?? "Impossible de résoudre les documents du patron.");

            entity.Name = name;
            entity.Url = NormalizeOptional(request.Url);
            entity.Note = NormalizeOptional(request.Note);
            entity.Documents.Clear();
            foreach (var document in documents.Value!)
                entity.Documents.Add(document);

            await context.SaveChangesAsync();
            return ResultT<Pattern>.Ok(entity.ToDomain());
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de mettre à jour le patron {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de mettre à jour le patron {request.Id}: {ex.Message}");
        }
    }

    public async Task<Result> AddDocumentAsync(int patternId, Guid documentId)
    {
        try
        {
            var pattern = await context.Patterns
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PatternId == patternId);
            if (pattern is null)
                return Result.NotFound($"Le patron {patternId} est introuvable.");

            var document = await context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound($"Le document {documentId} est introuvable.");

            if (pattern.Documents.Any(d => d.DocumentId == documentId))
                return Result.Ok();

            pattern.Documents.Add(document);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible d'ajouter le document au patron {patternId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible d'ajouter le document au patron {patternId}: {ex.Message}");
        }
    }

    public async Task<Result> RemoveDocumentAsync(int patternId, Guid documentId)
    {
        try
        {
            var pattern = await context.Patterns
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PatternId == patternId);
            if (pattern is null)
                return Result.NotFound($"Le patron {patternId} est introuvable.");

            var document = pattern.Documents.FirstOrDefault(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound($"Le document {documentId} n'est pas lié à ce patron.");

            pattern.Documents.Remove(document);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de retirer le document du patron {patternId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de retirer le document du patron {patternId}: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var entity = await context.Patterns.FindAsync(id);
            if (entity is null)
                return Result.NotFound($"Le patron {id} est introuvable.");

            context.Patterns.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer le patron {id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer le patron {id}: {ex.Message}");
        }
    }

    private async Task<ResultT<IReadOnlyList<DocumentEntity>>> ResolveDocumentsAsync(IReadOnlyCollection<Guid> documentIds)
    {
        var ids = documentIds.Distinct().ToList();
        if (ids.Count == 0)
            return ResultT<IReadOnlyList<DocumentEntity>>.Ok([]);

        var documents = await context.Documents
            .Where(d => ids.Contains(d.DocumentId))
            .ToListAsync();

        if (documents.Count != ids.Count)
        {
            var missingIds = ids.Except(documents.Select(d => d.DocumentId)).ToList();
            return ResultT<IReadOnlyList<DocumentEntity>>.NotFound(
                $"Certains documents sont introuvables: {string.Join(", ", missingIds)}");
        }

        return ResultT<IReadOnlyList<DocumentEntity>>.Ok(documents);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
