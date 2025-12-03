using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;

    public ExpenseService(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
        => _expenseRepository.GetAllAsync(cancellationToken);

    public Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _expenseRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _expenseRepository.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public async Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        _expenseRepository.Update(expense);
        await _expenseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        _expenseRepository.Remove(entity);
        await _expenseRepository.SaveChangesAsync(cancellationToken);
    }
}
