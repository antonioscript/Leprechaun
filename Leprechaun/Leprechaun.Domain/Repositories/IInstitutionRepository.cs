using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface IInstitutionRepository
{
    Task<List<Institution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Institution?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Institution institution, CancellationToken cancellationToken = default);
    void Update(Institution institution);
    void Remove(Institution institution);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}