using Leprechaun.Application.Models;
using Leprechaun.Application.Telegram;
using Leprechaun.Application.Telegram.Flows;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramSender _telegramSender;
    private readonly IPersonService _personService;
    private readonly IChatStateService _chatStateService;
    private readonly IEnumerable<IChatFlow> _flows;

    public TelegramController(
        ITelegramSender telegramSender,
        IPersonService personService,
        IChatStateService chatStateService,
        IEnumerable<IChatFlow> flows)
    {
        _telegramSender = telegramSender;
        _personService = personService;
        _chatStateService = chatStateService;
        _flows = flows;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (!HasValidMessage(update))
            return Ok();

        var chatId = update.Message!.Chat.Id;
        var userText = update.Message.Text!.Trim();

        var command = TelegramCommandParser.Parse(userText);
        var state = await GetOrCreateStateAsync(chatId, cancellationToken);

        // 1) /cancelar é global
        if (command == TelegramCommand.Cancelar)
        {
            await HandleCancelAsync(chatId, state, cancellationToken);
            return Ok();
        }

        // 2) Tentar fluxos (salário, depois outros)
        var handledByFlow = await TryHandleFlowsAsync(chatId, userText, state, command, cancellationToken);
        if (handledByFlow)
            return Ok();

        // 3) Comandos simples (start, help, ping, person…)
        var handledSimple = await TryHandleSimpleCommandAsync(chatId, command, cancellationToken);
        if (handledSimple)
            return Ok();

        // 4) Fallback
        await SendUnknownCommandAsync(chatId, cancellationToken);
        return Ok();
    }

    // ==========================
    // Métodos privados
    // ==========================

    private static bool HasValidMessage(TelegramUpdate update)
        => update.Message is { Text: not null } && !string.IsNullOrWhiteSpace(update.Message.Text);

    private async Task<ChatState> GetOrCreateStateAsync(long chatId, CancellationToken cancellationToken)
    {
        var state = await _chatStateService.GetAsync(chatId, cancellationToken);
        if (state is not null)
            return state;

        return new ChatState
        {
            ChatId = chatId,
            State = FlowStates.Idle
        };
    }

    private async Task HandleCancelAsync(long chatId, ChatState state, CancellationToken cancellationToken)
    {
        if (state.State != FlowStates.Idle)
        {
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            await _telegramSender.SendMessageAsync(
                chatId,
                "✅ Fluxo atual cancelado. Você pode começar outro comando quando quiser.",
                cancellationToken);
        }
        else
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Não há nenhum fluxo em andamento para cancelar.",
                cancellationToken);
        }
    }

    private async Task<bool> TryHandleFlowsAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        foreach (var flow in _flows)
        {
            var handled = await flow.TryHandleAsync(chatId, userText, state, command, cancellationToken);
            if (handled)
                return true;
        }

        return false;
    }

    private async Task<bool> TryHandleSimpleCommandAsync(
        long chatId,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        switch (command)
        {
            case TelegramCommand.Start:
                await _telegramSender.SendMessageAsync(
                    chatId,
                    BotTexts.Welcome(),
                    cancellationToken);
                return true;

            case TelegramCommand.Help:
                await _telegramSender.SendMessageAsync(
                    chatId,
                    BotTexts.Help(),
                    cancellationToken);
                return true;

            case TelegramCommand.Ping:
                await _telegramSender.SendMessageAsync(
                    chatId,
                    "Pong! 🏓",
                    cancellationToken);
                return true;

            case TelegramCommand.Person:
                var persons = await _personService.GetAllAsync(cancellationToken);
                var msg = BotTexts.PersonsList(persons);
                await _telegramSender.SendMessageAsync(chatId, msg, cancellationToken);
                return true;

            default:
                return false;
        }
    }

    private async Task SendUnknownCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.UnknownCommand(),
            cancellationToken);
    }
}
