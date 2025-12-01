using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<List<PersonResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var persons = await _personRepository.GetAllAsync(cancellationToken);

        return persons.Select(p => new PersonResponse
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive
        }).ToList();
    }
}