using Leprechaun.Application.DTOs;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("expenses")]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpenseController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var expense = new Expense
        {
            CostCenterId = request.CostCenterId,
            Name = request.Name,
            Description = request.Description,
            DefaultAmount = request.DefaultAmount,
            DueDay = request.DueDay,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive
        };

        var created = await _expenseService.CreateAsync(expense, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var expense = await _expenseService.GetByIdAsync(id, cancellationToken);
        if (expense is null)
            return NotFound();

        return Ok(expense);
    }
}