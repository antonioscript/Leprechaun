using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class CostCenterRepository : ICostCenterRepository
{
    private readonly LeprechaunDbContext _context;

    public CostCenterRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<CostCenter>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.CostCenters.AsNoTracking().ToListAsync(cancellationToken);

    public Task<CostCenter?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.CostCenters.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        await _context.CostCenters.AddAsync(costCenter, cancellationToken);
    }

    public void Update(CostCenter costCenter)
    {
        _context.CostCenters.Update(costCenter);
    }

    public void Remove(CostCenter costCenter)
    {
        _context.CostCenters.Remove(costCenter);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}