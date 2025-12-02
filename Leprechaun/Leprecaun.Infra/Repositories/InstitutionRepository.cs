using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class InstitutionRepository : IInstitutionRepository
{
    private readonly LeprechaunDbContext _context;

    public InstitutionRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<Institution>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.Institutions.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Institution?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Institutions.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task AddAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        await _context.Institutions.AddAsync(institution, cancellationToken);
    }

    public void Update(Institution institution)
    {
        _context.Institutions.Update(institution);
    }

    public void Remove(Institution institution)
    {
        _context.Institutions.Remove(institution);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}