using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;

    public ExpenseService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<List<Expense>> GetByCostCenterAsync(int costCenterId, CancellationToken cancellationToken = default)
        => _repository.GetByCostCenterAsync(costCenterId, cancellationToken);

    public Task<List<Expense>> GetByCostCenterNotDescriptionAsync(int costCenterId, CancellationToken cancellationToken = default)
        => _repository.GetByCostCenterNotDescriptionAsync(costCenterId, cancellationToken);

    public async Task<Expense> CreateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(expense, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public async Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        _repository.Update(expense);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
