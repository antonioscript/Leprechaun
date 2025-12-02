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
    private readonly SalaryIncomeFlowService _salaryIncomeFlow;

    public TelegramController(
        ITelegramSender telegramSender,
        IPersonService personService,
        IChatStateService chatStateService,
        SalaryIncomeFlowService salaryIncomeFlow)
    {
        _telegramSender = telegramSender;
        _personService = personService;
        _chatStateService = chatStateService;
        _salaryIncomeFlow = salaryIncomeFlow;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (update.Message is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return Ok();

        var chatId = update.Message.Chat.Id;
        var userText = update.Message.Text.Trim();

        // 1) Descobrir comando
        var command = TelegramCommandParser.Parse(userText);

        // 2) Carregar ou criar estado
        var state = await _chatStateService.GetAsync(chatId, cancellationToken)
                    ?? new ChatState { ChatId = chatId, State = FlowStates.Idle };

        // 3) Tentar deixar o fluxo de salário tratar
        var handledBySalaryFlow = await _salaryIncomeFlow.TryHandleAsync(
            chatId, userText, state, command, cancellationToken);

        if (handledBySalaryFlow)
            return Ok();

        // 4) Comandos simples (fora de fluxo)

        switch (command)
        {
            case TelegramCommand.Start:
                await _telegramSender.SendMessageAsync(chatId, BotTexts.Welcome(), cancellationToken);
                return Ok();

            case TelegramCommand.Help:
                await _telegramSender.SendMessageAsync(chatId, BotTexts.Help(), cancellationToken);
                return Ok();

            case TelegramCommand.Ping:
                await _telegramSender.SendMessageAsync(chatId, "Pong! 🏓", cancellationToken);
                return Ok();

            case TelegramCommand.Person:
                var persons = await _personService.GetAllAsync(cancellationToken);
                var msg = BotTexts.PersonsList(persons);
                await _telegramSender.SendMessageAsync(chatId, msg, cancellationToken);
                return Ok();
        }

        // 5) Fallback
        await _telegramSender.SendMessageAsync(chatId, BotTexts.UnknownCommand(), cancellationToken);
        return Ok();
    }
}