using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Expense>> GetByCostCenterAsync(int costCenterId, CancellationToken cancellationToken = default);

    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    void Update(Expense expense);
    void Remove(Expense expense);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
