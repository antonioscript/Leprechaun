using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly LeprechaunDbContext _context;

    public PersonRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<Person>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.Persons.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Persons.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        await _context.Persons.AddAsync(person, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}