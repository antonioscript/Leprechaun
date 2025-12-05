using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;
public interface ISupportSuggestionRepository
{
    Task AddAsync(SupportSuggestion suggestion, CancellationToken cancellationToken = default);
    Task<List<SupportSuggestion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}