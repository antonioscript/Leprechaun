using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Interfaces;

public interface IChatStateService
{
    Task<ChatState?> GetAsync(long chatId, CancellationToken cancellationToken = default);
    Task SaveAsync(ChatState state, CancellationToken cancellationToken = default);
    Task ClearAsync(long chatId, CancellationToken cancellationToken = default);
}