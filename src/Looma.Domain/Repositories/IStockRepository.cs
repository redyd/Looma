using Looma.Domain.Entities;
using Looma.Domain.Core;

namespace Looma.Domain.Repositories;

public interface IStockRepository
{
    Task<ResultT<IReadOnlyList<Stock>>> GetByWoolIdAsync(int woolId);
    Task<ResultT<double>> GetTotalWeightByWoolIdAsync(int woolId);
    Task<ResultT<Stock>> AddAsync(Stock stock);
    Task<ResultT<Stock>> UpdateAsync(Stock stock);
    Task<Result> DeleteAsync(int stockId);
}
