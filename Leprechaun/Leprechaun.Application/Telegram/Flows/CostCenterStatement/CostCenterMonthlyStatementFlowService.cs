using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.CostCenterStatement;

public class CostCenterMonthlyStatementFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public CostCenterMonthlyStatementFlowService(
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
        if (state.State == FlowStates.CostCenterStatementAwaitingPerson ||
            state.State == FlowStates.CostCenterStatementAwaitingCenter)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        if (command == TelegramCommand.ExtratoCaixinhaMes)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    // --------------------- INÍCIO DO FLUXO ---------------------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.CostCenterStatementAwaitingPerson;
        state.TempPersonId = null;
        state.TempSourceCostCenterId = null;
        state.TempAmount = null;
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

        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            "👤 *Selecione o titular da caixinha para ver o extrato do mês:*",
            buttons,
            cancellationToken);
    }

    // --------------------- CONTINUAÇÃO ---------------------

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        switch (state.State)
        {
            case FlowStates.CostCenterStatementAwaitingPerson:
                await HandlePersonAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterStatementAwaitingCenter:
                await HandleCenterAsync(chatId, userText, state, cancellationToken);
                break;
        }
    }

    private async Task HandlePersonAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var personId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular inválido. Clique novamente em um botão.",
                cancellationToken);
            return;
        }

        state.TempPersonId = personId;
        state.State = FlowStates.CostCenterStatementAwaitingCenter;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var centers = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId)
            .ToList();

        if (!centers.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Esse titular não possui caixinhas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var buttons = centers
            .Select(c => (Label: c.Name, Data: c.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            "📦 *Selecione a caixinha para ver o extrato do mês:*",
            buttons,
            cancellationToken);
    }

    private async Task HandleCenterAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: titular não definido. Recomece com /extrato_caixinha_mes.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        if (!int.TryParse(userText, out var centerId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Caixinha inválida. Clique novamente em um botão.",
                cancellationToken);
            return;
        }

        var personId = state.TempPersonId.Value;
        state.TempSourceCostCenterId = centerId;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        // Dados básicos
        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault(p => p.Id == personId);

        var centers = await _costCenterService.GetAllAsync(cancellationToken);
        var center = centers.FirstOrDefault(c => c.Id == centerId);

        if (center is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Caixinha não encontrada.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        // Saldo atual da caixinha
        var balance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        // Período: mês atual (do primeiro dia até agora)
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Todas as transações e filtragem de despesas da caixinha no mês
        var allTx = await _transactionService.GetAllAsync(cancellationToken);

        var monthExpenses = allTx
            .Where(t =>
                t.SourceCostCenterId == centerId &&
                t.TransactionType == "Expense" &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("📊 *Extrato da caixinha (mês atual)*");
        sb.AppendLine($"👤 Titular: *{person?.Name}*");
        sb.AppendLine($"📦 Caixinha: *{center.Name}*");
        sb.AppendLine();
        sb.AppendLine($"💰 Saldo atual da caixinha: *R$ {balance:N2}*");
        sb.AppendLine();

        if (!monthExpenses.Any())
        {
            sb.AppendLine("Não há despesas registradas para esta caixinha neste mês.");
        }
        else
        {
            sb.AppendLine("🧾 *Despesas no mês:*");
            sb.AppendLine();

            foreach (var tx in monthExpenses)
            {
                var desc = string.IsNullOrWhiteSpace(tx.Description)
                    ? "Sem descrição"
                    : tx.Description;

                var dateLocal = tx.TransactionDate.ToLocalTime();

                sb.AppendLine(
                    $"- R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
            }

            var total = monthExpenses.Sum(t => t.Amount);
            sb.AppendLine();
            sb.AppendLine($"💸 *Total de despesas no mês:* R$ {total:N2}");
        }

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);
    }
}
