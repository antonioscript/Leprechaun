using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class FinanceTransactionService : IFinanceTransactionService
{
    private readonly IFinanceTransactionRepository _transactionRepository;

    public FinanceTransactionService(IFinanceTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    // ---------- CRUD básico ----------

    public Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => _transactionRepository.GetAllAsync(cancellationToken);

    public Task<FinanceTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _transactionRepository.GetByIdAsync(id, cancellationToken);

    // ---------- Helpers de saldo ----------

    public async Task<decimal> GetSalaryAccumulatedAsync(int personId, CancellationToken cancellationToken = default)
    {
        var all = await _transactionRepository.GetAllAsync(cancellationToken);

        var incomesToLiquidity = all
            .Where(t => t.PersonId == personId
                        && t.TransactionType == "Income"
                        && t.TargetCostCenterId == null)
            .Sum(t => t.Amount);

        var outflowsFromLiquidity = all
            .Where(t => t.PersonId == personId
                        && t.SourceCostCenterId == null
                        && (t.TransactionType == "Expense" || t.TransactionType == "Transfer"))
            .Sum(t => t.Amount);

        return incomesToLiquidity - outflowsFromLiquidity;
    }

    public async Task<decimal> GetCostCenterBalanceAsync(int costCenterId, CancellationToken cancellationToken = default)
    {
        var all = await _transactionRepository.GetAllAsync(cancellationToken);

        var entries = all
            .Where(t => t.TargetCostCenterId == costCenterId)
            .Sum(t => t.Amount);

        var exits = all
            .Where(t => t.SourceCostCenterId == costCenterId)
            .Sum(t => t.Amount);

        return entries - exits;
    }

    public async Task<decimal> GetTotalSalaryAccumulatedAsync(CancellationToken cancellationToken = default)
    {
        var all = await _transactionRepository.GetAllAsync(cancellationToken);

        // Entradas de salário (sem cost center → liquidez)
        var totalIncome = all
            .Where(t => t.TransactionType == "Income" && t.TargetCostCenterId == null)
            .Sum(t => t.Amount);

        // Saídas feitas direto da liquidez
        var totalOutflow = all
            .Where(t => (t.TransactionType == "Expense" || t.TransactionType == "Transfer")
                        && t.SourceCostCenterId == null)
            .Sum(t => t.Amount);

        return totalIncome - totalOutflow;
    }

    public async Task<DateTime?> GetLastSalaryAccumulatedUpdateAsync(CancellationToken cancellationToken = default)
    {
        var all = await _transactionRepository.GetAllAsync(cancellationToken);

        // Mesma lógica de "o que afeta salário acumulado"
        var relevant = all
            .Where(t =>
                (t.TransactionType == "Income" && t.TargetCostCenterId == null) ||
                ((t.TransactionType == "Expense" || t.TransactionType == "Transfer")
                 && t.SourceCostCenterId == null))
            .OrderByDescending(t => t.TransactionDate)
            .FirstOrDefault();

        return relevant?.TransactionDate;
    }

    // ---------- Operações de negócio ----------

    public async Task<FinanceTransaction> RegisterIncomeAsync(
        int personId,
        int institutionId,
        decimal amount,
        DateTime? date,
        int? targetCostCenterId,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var tx = new FinanceTransaction
        {
            PersonId = personId,
            InstitutionId = institutionId,
            Amount = amount,
            TransactionDate = date ?? DateTime.UtcNow,
            TransactionType = "Income",
            SourceCostCenterId = null,
            TargetCostCenterId = targetCostCenterId, // null => salary accumulated; not null => cost center
            CategoryId = categoryId,
            Description = description
        };

        await _transactionRepository.AddAsync(tx, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return tx;
    }

    public async Task<FinanceTransaction> RegisterExpenseFromSalaryAsync(
        int personId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var currentBalance = await GetSalaryAccumulatedAsync(personId, cancellationToken);
        if (currentBalance < amount)
            throw new InvalidOperationException("Insufficient salary accumulated balance.");

        var tx = new FinanceTransaction
        {
            PersonId = personId,
            Amount = amount,
            TransactionDate = date ?? DateTime.UtcNow,
            TransactionType = "Expense",
            SourceCostCenterId = null,       // from liquidity
            TargetCostCenterId = null,       // to third party
            InstitutionId = null,
            CategoryId = categoryId,
            Description = description
        };

        await _transactionRepository.AddAsync(tx, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return tx;
    }

    public async Task<FinanceTransaction> RegisterExpenseFromCostCenterAsync(
        int personId,
        int costCenterId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var balance = await GetCostCenterBalanceAsync(costCenterId, cancellationToken);
        if (balance < amount)
            throw new InvalidOperationException("Insufficient cost center balance.");

        var tx = new FinanceTransaction
        {
            PersonId = personId,
            Amount = amount,
            TransactionDate = date ?? DateTime.UtcNow,
            TransactionType = "Expense",
            SourceCostCenterId = costCenterId,  // from cost center
            TargetCostCenterId = null,         // to third party
            InstitutionId = null,
            CategoryId = categoryId,
            Description = description
        };

        await _transactionRepository.AddAsync(tx, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return tx;
    }

    public async Task<FinanceTransaction> TransferBetweenCostCentersAsync(
        int personId,
        int sourceCostCenterId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (sourceCostCenterId == targetCostCenterId)
            throw new ArgumentException("Source and target cost centers must be different.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var balance = await GetCostCenterBalanceAsync(sourceCostCenterId, cancellationToken);
        if (balance < amount)
            throw new InvalidOperationException("Insufficient source cost center balance.");

        var tx = new FinanceTransaction
        {
            PersonId = personId,
            Amount = amount,
            TransactionDate = date ?? DateTime.UtcNow,
            TransactionType = "Transfer",
            SourceCostCenterId = sourceCostCenterId,
            TargetCostCenterId = targetCostCenterId,
            InstitutionId = null,
            CategoryId = null,
            Description = description
        };

        await _transactionRepository.AddAsync(tx, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return tx;
    }

    // 👇 NOVO: transferência do salário acumulado para caixinha
    public async Task<FinanceTransaction> TransferFromSalaryToCostCenterAsync(
        int personId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var currentBalance = await GetSalaryAccumulatedAsync(personId, cancellationToken);
        if (currentBalance < amount)
            throw new InvalidOperationException("Insufficient salary accumulated balance.");

        var tx = new FinanceTransaction
        {
            PersonId = personId,
            Amount = amount,
            TransactionDate = date ?? DateTime.UtcNow,
            TransactionType = "Transfer",
            SourceCostCenterId = null,              // 👈 sai da liquidez (salário acumulado)
            TargetCostCenterId = targetCostCenterId,
            InstitutionId = null,
            CategoryId = null,
            Description = description
        };

        await _transactionRepository.AddAsync(tx, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return tx;
    }
}
