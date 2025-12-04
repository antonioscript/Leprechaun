using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IInstitutionService
{
    Task<List<Institution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Institution?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Institution> CreateAsync(Institution institution, CancellationToken cancellationToken = default);
    Task UpdateAsync(Institution institution, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Institution?> DeactivateAsync(int id, DateTime endDate, CancellationToken cancellationToken = default);

}