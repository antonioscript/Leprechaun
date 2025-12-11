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
            .ToListAsync(cancellationToken);

    public Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<Expense>> GetByCostCenterAsync(int costCenterId, CancellationToken cancellationToken = default)
        => _context.Expenses
            .AsNoTracking()
            .Where(e => e.CostCenterId == costCenterId)
            .ToListAsync(cancellationToken);

    public Task<List<Expense>> GetByCostCenterNotDescriptionAsync(int costCenterId, CancellationToken cancellationToken = default)
        => _context.Expenses
            .AsNoTracking()
            .Where(e => e.CostCenterId == costCenterId && e.RequiresCustomDescription == false)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _context.Expenses.AddAsync(expense, cancellationToken);
    }

    public void Update(Expense expense)
    {
        _context.Expenses.Update(expense);
    }

    public void Remove(Expense expense)
    {
        _context.Expenses.Remove(expense);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
