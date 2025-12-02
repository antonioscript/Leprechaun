using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class FinanceTransactionRepository : IFinanceTransactionRepository
{
    private readonly LeprechaunDbContext _context;

    public FinanceTransactionRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.FinanceTransactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<FinanceTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _context.FinanceTransactions.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task AddAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.FinanceTransactions.AddAsync(transaction, cancellationToken);
    }

    public void Update(FinanceTransaction transaction)
    {
        _context.FinanceTransactions.Update(transaction);
    }

    public void Remove(FinanceTransaction transaction)
    {
        _context.FinanceTransactions.Remove(transaction);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}