using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface ICostCenterRepository
{
    Task<List<CostCenter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CostCenter?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    void Update(CostCenter costCenter);
    void Remove(CostCenter costCenter);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}