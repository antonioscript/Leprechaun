using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface IPersonRepository
{
    Task<List<Person>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Person person, CancellationToken cancellationToken = default);
    void Update(Person person);
    void Remove(Person person);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}