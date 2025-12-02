using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface ICostCenterService
{
    Task<List<CostCenter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CostCenter?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CostCenter> CreateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}