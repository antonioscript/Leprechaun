using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => _categoryRepository.GetAllAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _categoryRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        _categoryRepository.Remove(entity);
        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }
}