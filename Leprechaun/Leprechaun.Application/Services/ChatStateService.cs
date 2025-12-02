using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Repositories;

namespace Leprechaun.Application.Services;

public class ChatStateService : IChatStateService
{
    private readonly IChatStateRepository _repo;

    public ChatStateService(IChatStateRepository repo)
    {
        _repo = repo;
    }

    public Task<ChatState?> GetAsync(long chatId, CancellationToken cancellationToken = default)
        => _repo.GetByChatIdAsync(chatId, cancellationToken);

    public Task SaveAsync(ChatState state, CancellationToken cancellationToken = default)
        => _repo.SaveAsync(state, cancellationToken);

    public Task ClearAsync(long chatId, CancellationToken cancellationToken = default)
        => _repo.ClearStateAsync(chatId, cancellationToken);
}