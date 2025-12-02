using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersonRepository _personRepository;

    public PersonsController(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Person>>> GetAll(CancellationToken cancellationToken)
    {
        var persons = await _personRepository.GetAllAsync(cancellationToken);
        return Ok(persons);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Person>> GetById(int id, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person is null)
            return NotFound();

        return Ok(person);
    }

    [HttpPost]
    public async Task<ActionResult<Person>> Create([FromBody] Person model, CancellationToken cancellationToken)
    {
        await _personRepository.AddAsync(model, cancellationToken);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }
    
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Person model, CancellationToken cancellationToken)
    {
        var existing = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        existing.Name = model.Name;
        existing.IsActive = model.IsActive;

        _personRepository.Update(existing);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        _personRepository.Remove(existing);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    
}