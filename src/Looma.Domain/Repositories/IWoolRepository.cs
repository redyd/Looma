using Looma.Domain.Entities;
using Looma.Domain.Core;

namespace Looma.Domain.Repositories;

public interface IWoolRepository
{
    Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync();
    Task<ResultT<IReadOnlyList<Wool>>> SearchAsync(string query);
    Task<ResultT<Wool>> GetByIdAsync(int id);
    Task<ResultT<Wool>> AddAsync(Wool wool);
    Task<ResultT<Wool>> UpdateAsync(Wool wool);
    Task<Result> DeleteAsync(int id);
}
