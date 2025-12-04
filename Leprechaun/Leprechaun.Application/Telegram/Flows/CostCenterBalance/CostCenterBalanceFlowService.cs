using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.CostCenterBalance;

public class CostCenterBalanceFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public CostCenterBalanceFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        ICostCenterService costCenterService,
        IFinanceTransactionService transactionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _costCenterService = costCenterService;
        _transactionService = transactionService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        // Já está dentro do fluxo?
        if (state.State == FlowStates.CostCenterBalanceAwaitingPerson)
        {
            await HandlePersonSelectedAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // Comando para iniciar o fluxo
        if (command == TelegramCommand.SaldoCaixinhas)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    // ---------- Início do fluxo ----------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.CostCenterBalanceAwaitingPerson;
        state.TempPersonId = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var persons = (await _personService.GetAllAsync(cancellationToken)).ToList();
        if (!persons.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não há titulares cadastrados.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = "👤 Selecione o titular para ver o *saldo das caixinhas*:";

        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    // ---------- Tratamento do titular escolhido ----------

    private async Task HandlePersonSelectedAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var personId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular inválido. Tente clicar novamente no botão.",
                cancellationToken);
            return;
        }

        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault(p => p.Id == personId);
        if (person is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular não encontrado. Tente novamente.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var costCenters = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId)
            .ToList();

        if (!costCenters.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                $"📦 O titular {person.Name} não possui caixinhas cadastradas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("📦 Saldos das caixinhas");
        sb.AppendLine($"Titular: {person.Name}");
        sb.AppendLine();

        decimal total = 0m;

        foreach (var cc in costCenters)
        {
            var balance = await _transactionService.GetCostCenterBalanceAsync(cc.Id, cancellationToken);
            total += balance;
            sb.AppendLine($"- {cc.Name}: R$ {balance:N2}");
        }

        sb.AppendLine();
        sb.AppendLine($"💰 Total em caixinhas: R$ {total:N2}");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);
    }
}
