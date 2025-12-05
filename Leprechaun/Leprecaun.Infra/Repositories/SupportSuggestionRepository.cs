// Leprecaun.Infra/Repositories/SupportSuggestionRepository.cs
using Leprecaun.Infra.Context;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leprecaun.Infra.Repositories;

public class SupportSuggestionRepository : ISupportSuggestionRepository
{
    private readonly LeprechaunDbContext _context;

    public SupportSuggestionRepository(LeprechaunDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SupportSuggestion suggestion, CancellationToken cancellationToken = default)
    {
        await _context.SupportSuggestions.AddAsync(suggestion, cancellationToken);
    }

    public Task<List<SupportSuggestion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.SupportSuggestions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
