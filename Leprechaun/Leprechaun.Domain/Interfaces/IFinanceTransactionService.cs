using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IFinanceTransactionService
{
    // ---------- CRUD básico ----------
    Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinanceTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    // ---------- Helpers de saldo ----------
    Task<decimal> GetSalaryAccumulatedAsync(int personId, CancellationToken cancellationToken = default);
    Task<decimal> GetCostCenterBalanceAsync(int costCenterId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSalaryAccumulatedAsync(CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSalaryAccumulatedUpdateAsync(CancellationToken cancellationToken = default);

    // ---------- Operações de negócio ----------
    Task<FinanceTransaction> RegisterIncomeAsync(
        int personId,
        int institutionId,
        decimal amount,
        DateTime? date,
        int? targetCostCenterId,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    Task<FinanceTransaction> RegisterExpenseFromSalaryAsync(
        int personId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    Task<FinanceTransaction> RegisterExpenseFromCostCenterAsync(
        int personId,
        int costCenterId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    Task<FinanceTransaction> TransferBetweenCostCentersAsync(
        int personId,
        int sourceCostCenterId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default);

    // Transferência do salário acumulado para caixinha
    Task<FinanceTransaction> TransferFromSalaryToCostCenterAsync(
        int personId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default);
}
