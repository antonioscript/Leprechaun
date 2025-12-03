using Leprechaun.Domain.Entities;

namespace Leprechaun.Application.Telegram.Flows;

public interface IChatFlow
{
    Task<bool> TryHandleAsync(long chatId, string userText, ChatState state, TelegramCommand command, CancellationToken cancellationToken);
}