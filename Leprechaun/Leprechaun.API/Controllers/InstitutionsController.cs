using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstitutionsController : ControllerBase
{
    private readonly IInstitutionService _institutionService;

    public InstitutionsController(IInstitutionService institutionService)
    {
        _institutionService = institutionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Institution>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _institutionService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Institution>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _institutionService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Institution>> Create([FromBody] Institution model, CancellationToken cancellationToken)
    {
        var created = await _institutionService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Institution model, CancellationToken cancellationToken)
    {
        var existing = await _institutionService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        // Atualiza campos que podem mudar
        existing.Name = model.Name;
        existing.Type = model.Type;
        existing.PersonId = model.PersonId;
        existing.Description = model.Description;
        existing.StartDate = model.StartDate;
        existing.EndDate = model.EndDate;
        existing.IsActive = model.IsActive;

        await _institutionService.UpdateAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _institutionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}