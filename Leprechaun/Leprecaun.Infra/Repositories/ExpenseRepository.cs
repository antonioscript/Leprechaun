using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly LeprechaunDbContext _context;

    public ExpenseRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.Expenses
            .AsNoTracking()
            .Include(e => e.CostCenter)
            .Include(e => e.Category)
            .ToListAsync(cancellationToken);

    public Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Expenses
            .Include(e => e.CostCenter)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        => _context.Expenses.AddAsync(expense, cancellationToken).AsTask();

    public void Update(Expense expense)
        => _context.Expenses.Update(expense);

    public void Remove(Expense expense)
        => _context.Expenses.Remove(expense);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
