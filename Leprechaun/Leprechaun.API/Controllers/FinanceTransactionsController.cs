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
        [FromQuery] DateTime? transactionDate,
        CancellationToken cancellationToken)
    {
        var date = transactionDate ?? DateTime.UtcNow;

        var tx = await _transactionService.RegisterExpenseFromSalaryAsync(
            personId,
            amount,
            date,
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
        [FromQuery] DateTime? transactionDate,
        CancellationToken cancellationToken)
    {
        var date = transactionDate ?? DateTime.UtcNow;

        var tx = await _transactionService.RegisterIncomeAsync(
            personId,
            institutionId,
            amount,
            date,
            targetCostCenterId: null,
            categoryId,
            description,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
    }

    // 🔹 Transferir do salário acumulado direto para uma caixinha
    [HttpPost("transfer-from-salary-to-costcenter")]
    public async Task<ActionResult<FinanceTransaction>> TransferFromSalaryToCostCenter(
        [FromQuery] int personId,
        [FromQuery] int targetCostCenterId,
        [FromQuery] decimal amount,
        [FromQuery] string? description,
        [FromQuery] DateTime? transactionDate,
        CancellationToken cancellationToken)
    {
        var date = transactionDate ?? DateTime.UtcNow;

        var tx = await _transactionService.TransferFromSalaryToCostCenterAsync(
            personId,
            targetCostCenterId,
            amount,
            date,
            description,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
    }

    // 🔹 Fluxo "doação → salário acumulado → caixinha"
    [HttpPost("donation-to-costcenter")]
    public async Task<ActionResult> RegisterDonationToCostCenter(
        [FromQuery] int personId,
        [FromQuery] int institutionId,      // Institution do tipo "Donation"
        [FromQuery] int targetCostCenterId,
        [FromQuery] decimal amount,
        [FromQuery] int? categoryId,
        [FromQuery] string? description,
        [FromQuery] DateTime? transactionDate,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var baseDescription = description ?? "Donation to cost center";
        var date = transactionDate ?? DateTime.UtcNow;

        // 1) INCOME → entra no salário acumulado
        var incomeTx = await _transactionService.RegisterIncomeAsync(
            personId,
            institutionId,
            amount,
            date,
            targetCostCenterId: null,          // vai primeiro para o salário acumulado
            categoryId,
            baseDescription,
            cancellationToken);

        // 2) TRANSFER → salário acumulado → caixinha
        var transferDescription = $"{baseDescription} (transfer from salary to cost center {targetCostCenterId})";

        var transferTx = await _transactionService.TransferFromSalaryToCostCenterAsync(
            personId,
            targetCostCenterId,
            amount,
            date,
            transferDescription,
            cancellationToken);

        return Ok(new
        {
            Income = incomeTx,
            Transfer = transferTx
        });
    }
}
