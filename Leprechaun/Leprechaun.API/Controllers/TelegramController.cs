using Leprechaun.API.Models;
using Leprechaun.API.Services;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramSender _telegramSender;
    private readonly IPersonService _personService;


    public TelegramController(
        ITelegramSender telegramSender,
        IPersonService personService)
    {
        _telegramSender = telegramSender;
        _personService = personService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (update.Message is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return Ok();

        var chatId = update.Message.Chat.Id;
        var userText = update.Message.Text.Trim();

        string replyText;

        // 1) Comando /start
        if (userText.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            replyText =
                "🍀 Olá! Eu sou o Leprechaun Bot.\n" +
                "Use /help para ver o que eu sei fazer.";
        }
        // 2) Comando /ping
        else if (userText.StartsWith("/ping", StringComparison.OrdinalIgnoreCase))
        {
            replyText = "Pong! 🏓";
        }
        // 3) Comando /help
        else if (userText.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            replyText =
                "📚 Comandos disponíveis:\n" +
                "/start - Mensagem de boas-vindas\n" +
                "/ping - Testa se o bot está online\n" +
                "/person - Lista os titulares da conta\n" +
                "/eco <texto> - Eu repito o texto que você enviar\n";
        }
        // 4) Comando /eco <texto>
        else if (userText.StartsWith("/eco", StringComparison.OrdinalIgnoreCase))
        {
            var args = userText.Substring(4).Trim(); // tudo depois de "/eco"
            if (string.IsNullOrWhiteSpace(args))
            {
                replyText = "Me diga o que você quer que eu repita. Ex: /eco bom dia!";
            }
            else
            {
                replyText = $"🔁 {args}";
            }
        }
        // 5) Comando /person - lista os titulares do banco
        else if (userText.StartsWith("/person", StringComparison.OrdinalIgnoreCase))
        {
            var persons = await _personService.GetAllAsync(cancellationToken);

            if (!persons.Any())
            {
                replyText = "Nenhum titular encontrado no banco.";
            }
            else
            {
                replyText = "👥 Titulares:\n";

                foreach (var p in persons)
                    replyText += $"• {p.Name}\n";
            }
        }
        // 5) Qualquer outra coisa
        else
        {
            replyText = "Não entendi 🤔. Use /help para ver os comandos disponíveis.";
        }

        await _telegramSender.SendMessageAsync(chatId, replyText, cancellationToken);

        return Ok();
    }
}