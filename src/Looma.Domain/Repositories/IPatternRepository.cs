using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface IPatternRepository
{
    Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync();
    Task<ResultT<Pattern>> GetByIdAsync(int id);
    Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request);
    Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request);
    Task<Result> AddDocumentAsync(int patternId, Guid documentId);
    Task<Result> RemoveDocumentAsync(int patternId, Guid documentId);
    Task<Result> DeleteAsync(int id);
}
