using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class CostCenterService : ICostCenterService
{
    private readonly ICostCenterRepository _costCenterRepository;

    public CostCenterService(ICostCenterRepository costCenterRepository)
    {
        _costCenterRepository = costCenterRepository;
    }

    public Task<List<CostCenter>> GetAllAsync(CancellationToken cancellationToken = default)
        => _costCenterRepository.GetAllAsync(cancellationToken);

    public Task<CostCenter?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _costCenterRepository.GetByIdAsync(id, cancellationToken);

    public async Task<CostCenter> CreateAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        await _costCenterRepository.AddAsync(costCenter, cancellationToken);
        await _costCenterRepository.SaveChangesAsync(cancellationToken);
        return costCenter;
    }

    public async Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        _costCenterRepository.Update(costCenter);
        await _costCenterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _costCenterRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        _costCenterRepository.Remove(entity);
        await _costCenterRepository.SaveChangesAsync(cancellationToken);
    }
}