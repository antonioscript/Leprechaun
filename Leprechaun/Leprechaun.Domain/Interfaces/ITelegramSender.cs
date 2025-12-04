namespace Leprechaun.Domain.Interfaces;

public interface ITelegramSender
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);

    // NOVO: enviar mensagem com inline keyboard
    Task SendMessageWithInlineKeyboardAsync(long chatId, string text, IEnumerable<(string Label, string Data)> buttons, CancellationToken cancellationToken = default);
}