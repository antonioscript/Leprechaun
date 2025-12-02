using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceTransactionsController : ControllerBase
{
    private readonly IFinanceTransactionService _transactionService;

    public FinanceTransactionsController(IFinanceTransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FinanceTransaction>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _transactionService.GetAllAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<FinanceTransaction>> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _transactionService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    // Exemplo: endpoint simples para registrar uma despesa a partir do salário acumulado
    [HttpPost("expense-from-salary")]
    public async Task<ActionResult<FinanceTransaction>> RegisterExpenseFromSalary(
        [FromQuery] int personId,
        [FromQuery] decimal amount,
        [FromQuery] int? categoryId,
        [FromQuery] string? description,
        CancellationToken cancellationToken)
    {
        var tx = await _transactionService.RegisterExpenseFromSalaryAsync(
            personId,
            amount,
            DateTime.UtcNow,
            categoryId,
            description,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
    }

    // Exemplo: registrar income simples indo para o salário acumulado
    [HttpPost("income-to-salary")]
    public async Task<ActionResult<FinanceTransaction>> RegisterIncomeToSalary(
        [FromQuery] int personId,
        [FromQuery] int institutionId,
        [FromQuery] decimal amount,
        [FromQuery] int? categoryId,
        [FromQuery] string? description,
        CancellationToken cancellationToken)
    {
        var tx = await _transactionService.RegisterIncomeAsync(
            personId,
            institutionId,
            amount,
            DateTime.UtcNow,
            targetCostCenterId: null,
            categoryId,
            description,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
    }
}