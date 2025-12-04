using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IFinanceTransactionService
{
    // CRUD básico (se precisar em telas)
    Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinanceTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    // Operações de negócio principais

    /// <summary>
    /// Registra uma RECEITA (Income).
    /// Se targetCostCenterId for null -> entra no salário acumulado.
    /// Se tiver targetCostCenterId -> entra direto na caixinha.
    /// </summary>
    Task<FinanceTransaction> RegisterIncomeAsync(
        int personId,
        int institutionId,
        decimal amount,
        DateTime? date,
        int? targetCostCenterId,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra DESPESA a partir do salário acumulado.
    /// (Origem = liquidez, não caixinha)
    /// </summary>
    Task<FinanceTransaction> RegisterExpenseFromSalaryAsync(
        int personId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra DESPESA a partir de uma caixinha.
    /// (Origem = CostCenter)
    /// </summary>
    Task<FinanceTransaction> RegisterExpenseFromCostCenterAsync(
        int personId,
        int costCenterId,
        decimal amount,
        DateTime? date,
        int? categoryId,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfere valor de uma caixinha para outra.
    /// </summary>
    Task<FinanceTransaction> TransferBetweenCostCentersAsync(
        int personId,
        int sourceCostCenterId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default);

    // Helpers de saldo (vamos usar no futuro e no /transacao)
    Task<decimal> GetSalaryAccumulatedAsync(int personId, CancellationToken cancellationToken = default);
    Task<decimal> GetCostCenterBalanceAsync(int costCenterId, CancellationToken cancellationToken = default);
    
    Task<decimal> GetTotalSalaryAccumulatedAsync(CancellationToken cancellationToken = default);

    Task<FinanceTransaction> TransferFromSalaryToCostCenterAsync(
        int personId,
        int targetCostCenterId,
        decimal amount,
        DateTime? date,
        string? description,
        CancellationToken cancellationToken = default);


    // última movimentação que afeta o salário acumulado
    Task<DateTime?> GetLastSalaryAccumulatedUpdateAsync(CancellationToken cancellationToken = default);

}