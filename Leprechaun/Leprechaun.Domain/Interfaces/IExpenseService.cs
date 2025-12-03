using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default);
    Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
