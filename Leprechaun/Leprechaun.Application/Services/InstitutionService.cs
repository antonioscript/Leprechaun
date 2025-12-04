using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class InstitutionService : IInstitutionService
{
    private readonly IInstitutionRepository _institutionRepository;

    public InstitutionService(IInstitutionRepository institutionRepository)
    {
        _institutionRepository = institutionRepository;
    }

    public Task<List<Institution>> GetAllAsync(CancellationToken cancellationToken = default)
        => _institutionRepository.GetAllAsync(cancellationToken);

    public Task<Institution?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _institutionRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Institution> CreateAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        await _institutionRepository.AddAsync(institution, cancellationToken);
        await _institutionRepository.SaveChangesAsync(cancellationToken);
        return institution;
    }

    public async Task UpdateAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        _institutionRepository.Update(institution);
        await _institutionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _institutionRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return;

        _institutionRepository.Remove(entity);
        await _institutionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<Institution?> DeactivateAsync(int id, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var inst = await _institutionRepository.GetByIdAsync(id, cancellationToken);
        if (inst == null)
            return null;

        inst.IsActive = false;
        inst.EndDate = endDate;

        _institutionRepository.Update(inst);
        await _institutionRepository.SaveChangesAsync(cancellationToken);

        return inst;
    }
}