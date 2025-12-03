using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _expenseService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Expense>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _expenseService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Expense>> Create([FromBody] Expense model, CancellationToken cancellationToken)
    {
        var created = await _expenseService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Expense model, CancellationToken cancellationToken)
    {
        var existing = await _expenseService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.DefaultAmount = model.DefaultAmount;
        existing.DueDay = model.DueDay;
        existing.CostCenterId = model.CostCenterId;
        existing.CategoryId = model.CategoryId;
        existing.IsActive = model.IsActive;

        await _expenseService.UpdateAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _expenseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
