using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService _costCenterService;

    public CostCentersController(ICostCenterService costCenterService)
    {
        _costCenterService = costCenterService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CostCenter>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _costCenterService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CostCenter>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _costCenterService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CostCenter>> Create([FromBody] CostCenter model, CancellationToken cancellationToken)
    {
        var created = await _costCenterService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CostCenter model, CancellationToken cancellationToken)
    {
        var existing = await _costCenterService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        existing.Name = model.Name;
        existing.PersonId = model.PersonId;
        existing.Description = model.Description;
        existing.IsActive = model.IsActive;

        await _costCenterService.UpdateAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _costCenterService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}