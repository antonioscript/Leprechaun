using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Enums;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.CostCenterStatement;

public class CostCenterMonthlyStatementFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly IExpenseService _expenseService;
    private readonly ITelegramSender _telegramSender;

    public CostCenterMonthlyStatementFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        ICostCenterService costCenterService,
        IFinanceTransactionService transactionService,
        IExpenseService expenseService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _costCenterService = costCenterService;
        _transactionService = transactionService;
        _expenseService = expenseService;
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

        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault(p => p.Id == personId);

        if (person is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular não encontrado.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var personName = person.Name;

        var centers = (await _costCenterService.GetAllAsync(cancellationToken)).ToList();
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

        // 🔹 Se for InfraMensal → relatório especial
        if (center.Type == CostCenterType.InfraMensal)
        {
            await HandleInfraMonthlyStatementAsync(
                chatId,
                personName,
                center,
                cancellationToken);
        }
        else
        {
            await HandleDefaultMonthlyStatementAsync(
                chatId,
                personName,
                center,
                centers,
                cancellationToken);
        }

        await _chatStateService.ClearAsync(chatId, cancellationToken);
    }

    // ===================== RELATÓRIO PADRÃO =====================

    private async Task HandleDefaultMonthlyStatementAsync(
        long chatId,
        string personName,
        CostCenter center,
        List<CostCenter> allCenters,
        CancellationToken cancellationToken)
    {
        var centerId = center.Id;

        var balance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allTx = await _transactionService.GetAllAsync(cancellationToken);

        var monthExpenses = allTx
            .Where(t =>
                t.SourceCostCenterId == centerId &&
                t.TransactionType == "Expense" &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var outgoingTransfers = allTx
            .Where(t =>
                t.TransactionType == "Transfer" &&
                t.SourceCostCenterId == centerId &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var incomingTransfers = allTx
            .Where(t =>
                t.TransactionType == "Transfer" &&
                t.TargetCostCenterId == centerId &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var centersById = allCenters.ToDictionary(c => c.Id, c => c.Name);

        var sb = new StringBuilder();
        sb.AppendLine("📊 *Extrato da caixinha (mês atual)*");
        sb.AppendLine($"👤 Titular: *{personName}*");
        sb.AppendLine($"📦 Caixinha: *{center.Name}*");
        sb.AppendLine();
        sb.AppendLine($"💰 Saldo atual da caixinha: *R$ {balance:N2}*");
        sb.AppendLine();

        // --- DESPESAS ---

        if (!monthExpenses.Any())
        {
            sb.AppendLine("🧾 *Despesas no mês:*");
            sb.AppendLine();
            sb.AppendLine("Nenhuma despesa registrada para esta caixinha neste mês.");
        }
        else
        {
            sb.AppendLine("🧾 *Despesas no mês:*\n");

            foreach (var tx in monthExpenses)
            {
                var desc = string.IsNullOrWhiteSpace(tx.Description)
                    ? "Sem descrição"
                    : tx.Description;

                var dateLocal = tx.TransactionDate.ToLocalTime();

                sb.AppendLine($"- R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
            }

            var totalExpenses = monthExpenses.Sum(t => t.Amount);
            sb.AppendLine();
            sb.AppendLine($"💸 *Total de despesas no mês:* R$ {totalExpenses:N2}");
        }

        // --- TRANSFERÊNCIAS INTERNAS ---

        if (outgoingTransfers.Any() || incomingTransfers.Any())
        {
            sb.AppendLine();
            sb.AppendLine("🔁 *Transferências internas no mês:*");
            sb.AppendLine();

            foreach (var tx in outgoingTransfers)
            {
                var dateLocal = tx.TransactionDate.ToLocalTime();
                var targetName = tx.TargetCostCenterId.HasValue &&
                                 centersById.TryGetValue(tx.TargetCostCenterId.Value, out var nameTarget)
                    ? nameTarget
                    : "Salário Acumulado";

                sb.AppendLine(
                    $"- R$ {tx.Amount:N2} | Transferência enviada para caixinha {targetName} | {dateLocal:dd/MM/yyyy}");
            }

            foreach (var tx in incomingTransfers)
            {
                var dateLocal = tx.TransactionDate.ToLocalTime();
                var sourceName = tx.SourceCostCenterId.HasValue &&
                                 centersById.TryGetValue(tx.SourceCostCenterId.Value, out var nameSource)
                    ? nameSource
                    : "Salário Acumulado";

                sb.AppendLine(
                    $"- R$ {tx.Amount:N2} | Transferência recebida de caixinha {sourceName} | {dateLocal:dd/MM/yyyy}");
            }

            var totalExpenses = monthExpenses.Sum(t => t.Amount);
            var totalTransfersOut = outgoingTransfers.Sum(t => t.Amount);
            var totalTransfersIn = incomingTransfers.Sum(t => t.Amount);
            var totalOut = totalExpenses + totalTransfersOut;

            sb.AppendLine();
            sb.AppendLine($"💸 *Total de despesas no mês:* R$ {totalExpenses:N2}");
            sb.AppendLine($"🔼 *Total transferido para outras caixinhas (saídas):* R$ {totalTransfersOut:N2}");
            sb.AppendLine($"🔽 *Total recebido de outras caixinhas (entradas):* R$ {totalTransfersIn:N2}");
            sb.AppendLine();
            sb.AppendLine($"📉 *Total de saídas (despesas + transferências enviadas):* R$ {totalOut:N2}");
        }

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }

    // ===================== RELATÓRIO ESPECIAL INFRA =====================

    private async Task HandleInfraMonthlyStatementAsync(
        long chatId,
        string personName,
        CostCenter center,
        CancellationToken cancellationToken)
    {
        var balance = await _transactionService.GetCostCenterBalanceAsync(center.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allTx = await _transactionService.GetAllAsync(cancellationToken);

        // Todas as despesas da caixinha no mês
        var monthExpenses = allTx
            .Where(t =>
                t.SourceCostCenterId == center.Id &&
                t.TransactionType == "Expense" &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .ToList();

        // Templates (Internet, Energia, etc.)
        var templates = await _expenseService.GetByCostCenterAsync(center.Id, cancellationToken);

        // Monta um "summary" por template
        var summaries = templates
            .Select(t =>
            {
                var txForTemplate = monthExpenses
                    .Where(tx =>
                           (tx.CategoryId.HasValue && t.CategoryId.HasValue && tx.CategoryId == t.CategoryId)
                           || string.Equals(tx.Description, t.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(tx => tx.TransactionDate)
                    .ToList();

                var totalPaid = txForTemplate.Sum(tx => tx.Amount);
                var expected = t.DefaultAmount ?? 0m;

                return new
                {
                    Template = t,
                    Transactions = txForTemplate,
                    TotalPaid = totalPaid,
                    Expected = expected
                };
            })
            // primeiro quem teve pagamento (> 0), depois os zerados
            .OrderBy(s => s.TotalPaid == 0 ? 1 : 0)
            .ThenBy(s => s.Template.Name)
            .ToList();

        // 🔹 Pega todas as transações que NÃO caíram em nenhum template
        var templatedTxIds = new HashSet<long>(
            summaries.SelectMany(s => s.Transactions).Select(tx => tx.Id));

        var untemplatedExpenses = monthExpenses
            .Where(tx => !templatedTxIds.Contains(tx.Id))
            .OrderBy(tx => tx.TransactionDate)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("📊 Extrato da caixinha de Infra Mensal (mês atual)");
        sb.AppendLine($"👤 Titular: {personName}");
        sb.AppendLine($"📦 Caixinha: {center.Name}");
        sb.AppendLine();
        sb.AppendLine($"💰 Saldo atual da caixinha: R$ {balance:N2}");
        sb.AppendLine();

        decimal totalExpected = 0m;
        decimal totalPaidAll = 0m;

        // -------- Templates (com emoji) --------
        foreach (var s in summaries)
        {
            totalExpected += s.Expected;
            totalPaidAll += s.TotalPaid;

            var comparisonText = BuildInfraComparisonText(s.Expected, s.TotalPaid, out var emoji);

            sb.AppendLine($"- {s.Template.Name} | R$ {s.TotalPaid:N2} | {emoji}");
            sb.AppendLine($"  (Esperado: R$ {s.Expected:N2})");

            // Parcelas individuais
            foreach (var tx in s.Transactions)
            {
                var dateLocal = tx.TransactionDate.ToLocalTime();
                var desc = string.IsNullOrWhiteSpace(tx.Description) ? "Sem descrição" : tx.Description;

                sb.AppendLine($"  └ R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
            }

            sb.AppendLine();
        }

        // -------- Despesas sem template --------
        if (untemplatedExpenses.Any())
        {
            var totalUntemplated = untemplatedExpenses.Sum(tx => tx.Amount);
            totalPaidAll += totalUntemplated;

            sb.AppendLine("🧾 Despesas sem template:");
            sb.AppendLine();

            foreach (var tx in untemplatedExpenses)
            {
                var dateLocal = tx.TransactionDate.ToLocalTime();
                var desc = string.IsNullOrWhiteSpace(tx.Description) ? "Sem descrição" : tx.Description;

                sb.AppendLine($"- R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("━━━━━━━━━━━━━━");
        sb.AppendLine($"📌 Total esperado no mês: R$ {totalExpected:N2}");
        sb.AppendLine($"📌 Total pago no mês: R$ {totalPaidAll:N2}");

        var overallText = BuildInfraComparisonText(totalExpected, totalPaidAll, out var overallEmoji);
        sb.AppendLine($"{overallEmoji} {overallText}");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }

    // Helper comparação esperado x pago
    private static string BuildInfraComparisonText(decimal expected, decimal paid, out string emoji)
    {
        if (expected <= 0)
        {
            emoji = "ℹ️";
            return "Valor esperado não configurado. Comparação não disponível.";
        }

        if (paid <= expected)
        {
            emoji = "🟢";
            return "Despesa dentro ou abaixo do valor esperado para este tipo de gasto.";
        }

        emoji = "🔴";
        return "Atenção: a despesa ficou acima do valor esperado para este tipo de gasto.";
    }
}
