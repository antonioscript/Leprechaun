using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface IFinanceTransactionRepository
{
    Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinanceTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task AddAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default);
    void Update(FinanceTransaction transaction);
    void Remove(FinanceTransaction transaction);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}