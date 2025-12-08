using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // ?? NOVO: buscar templates por caixinha (InfraMensal usa isso)
    Task<List<Expense>> GetByCostCenterAsync(int costCenterId, CancellationToken cancellationToken = default);

    Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default);
    Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
