using Leprechaun.API.Models;
using Leprechaun.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramSender _telegramSender;

    public TelegramController(ITelegramSender telegramSender)
    {
        _telegramSender = telegramSender;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        // Se não tiver mensagem ou texto, não faz nada
        if (update.Message is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return Ok();

        var chatId = update.Message.Chat.Id;
        var userText = update.Message.Text.Trim();

        // Regra mais simples possível: ecoar a mensagem
        var replyText = $"Você disse: {userText}";

        await _telegramSender.SendMessageAsync(chatId, replyText, cancellationToken);

        // Pro Telegram, basta responder 200 OK
        return Ok();
    }
}