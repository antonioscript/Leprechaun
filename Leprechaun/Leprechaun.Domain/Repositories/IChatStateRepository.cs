using Leprechaun.Domain.Entities;

namespace Leprechaun.Domain.Repositories;

public interface IChatStateRepository
{
    Task<ChatState?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken);
    Task SaveAsync(ChatState state, CancellationToken cancellationToken);
    Task ClearStateAsync(long chatId, CancellationToken cancellationToken);
}