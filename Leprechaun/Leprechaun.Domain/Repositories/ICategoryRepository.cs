using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Remove(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}