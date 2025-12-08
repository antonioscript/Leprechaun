using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Enums;

namespace Leprechaun.Domain.Interfaces;

public interface ICostCenterService
{
    Task<List<CostCenter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CostCenter?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CostCenter> CreateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    Task<CostCenter> CreateAsync(string name, int personId, CostCenterType type, CancellationToken cancellationToken = default);

    Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}