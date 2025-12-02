using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly LeprechaunDbContext _context;

    public CategoryRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.Categories.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Remove(Category category)
    {
        _context.Categories.Remove(category);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}