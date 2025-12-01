namespace Leprechaun.API.Services;

public interface ITelegramSender
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}