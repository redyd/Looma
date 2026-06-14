using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface IWoolUsageRepository
{
    Task<ResultT<WoolUsage>> GetUsageAsync(int projectId, int woolId);
    Task<Result> UpdateStockUsedAsync(int projectId, int woolId, double stockUsed);
    Task<Result> UpdateStockAlreadyUsedAsync(int projectId, int woolId, double stockAlreadyUsed);
    Task<Result> UpdateCurrentStockUsageAsync(int projectId, int woolId, double stockUsage);
}
