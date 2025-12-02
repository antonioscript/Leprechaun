namespace Leprechaun.Domain.Interfaces;

public interface ITelegramSender
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}