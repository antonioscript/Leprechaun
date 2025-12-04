// Leprechaun.Application/Services/SupportSuggestionService.cs
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class SupportSuggestionService : ISupportSuggestionService
{
    private readonly ISupportSuggestionRepository _repository;

    public SupportSuggestionService(ISupportSuggestionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SupportSuggestion> CreateAsync(
        long chatId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var suggestion = new SupportSuggestion
        {
            ChatId = chatId,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Source = "Telegram",
            Status = "Open"
        };

        await _repository.AddAsync(suggestion, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public Task<List<SupportSuggestion>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);
}
