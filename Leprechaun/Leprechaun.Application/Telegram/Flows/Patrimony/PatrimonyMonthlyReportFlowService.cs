using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Enums;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.Patrimony;

public class PatrimonyMonthlyReportFlowService : IChatFlow
{
    private readonly IFinanceTransactionService _transactionService;
    private readonly ICostCenterService _costCenterService;
    private readonly ITelegramSender _telegramSender;

    public PatrimonyMonthlyReportFlowService(
        IFinanceTransactionService transactionService,
        ICostCenterService costCenterService,
        ITelegramSender telegramSender)
    {
        _transactionService = transactionService;
        _costCenterService = costCenterService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (command != TelegramCommand.RelatorioPatrimonio)
            return false;

        await HandleReportAsync(chatId, cancellationToken);
        return true;
    }

    private async Task HandleReportAsync(
    long chatId,
    CancellationToken cancellationToken)
{
    var nowUtc = DateTime.UtcNow;
    var startOfMonthUtc = new DateTime(
        nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    // Datas para exibir no cabeçalho (local)
    var startLocal = startOfMonthUtc.ToLocalTime().Date;
    var endLocal = nowUtc.ToLocalTime().Date;

    var allTx = await _transactionService.GetAllAsync(cancellationToken);

    // Transações do período
    var periodTx = allTx
        .Where(t => t.TransactionDate >= startOfMonthUtc &&
                    t.TransactionDate <= nowUtc)
        .ToList();

    // ---------------- VISÃO GERAL ----------------

    // Entradas = todos "Income"
    var incomes = periodTx
        .Where(t => t.TransactionType == "Income")
        .ToList();

    // Saídas externas = despesas do salário (sem caixinha)
    var externalExpenses = periodTx
        .Where(t =>
            t.TransactionType == "Expense" &&
            t.SourceCostCenterId == null)
        .ToList();

    var totalIncomes = incomes.Sum(t => t.Amount);
    var totalExternalExpenses = externalExpenses.Sum(t => t.Amount);
    var saldoGlobal = totalIncomes - totalExternalExpenses;

    // ---------------- SALÁRIO ACUMULADO ----------------

    var salaryExpensesOrdered = externalExpenses
        .OrderBy(t => t.TransactionDate)
        .ToList();

    // ---------------- CAIXINHAS ----------------

    var allCenters = (await _costCenterService.GetAllAsync(cancellationToken)).ToList();

    var ccExpenseGroups = periodTx
        .Where(t =>
            t.TransactionType == "Expense" &&
            t.SourceCostCenterId.HasValue)
        .GroupBy(t => t.SourceCostCenterId!.Value)
        .ToList();

    var totalCcExpenses = ccExpenseGroups.Sum(g => g.Sum(x => x.Amount));

    // ---------------- MONTAGEM DO RELATÓRIO ----------------

    var sb = new StringBuilder();

    // Cabeçalho
    sb.AppendLine("📊 *Relatório de Patrimônio*");
    sb.AppendLine($"📅 Data: de {startLocal:dd/MM/yyyy} até {endLocal:dd/MM/yyyy}");
    sb.AppendLine();

    // Visão geral
    sb.AppendLine("🏦 *Visão geral (patrimônio)*");
    sb.AppendLine($"➡️ Entradas: R$ {totalIncomes:N2}");
    sb.AppendLine($"⬅️ Saídas (despesas externas, sem caixinhas): R$ {totalExternalExpenses:N2}");
    sb.AppendLine($"💰 Saldo: R$ {saldoGlobal:N2}");
    sb.AppendLine();

    // -------- SALÁRIO ACUMULADO (detalhado) --------
    sb.AppendLine("💼 *Salário acumulado*");
    sb.AppendLine($"➡️ Entradas: R$ {totalIncomes:N2}");
    sb.AppendLine($"⬅️ Saídas (apenas despesas externas): R$ {totalExternalExpenses:N2}");
    sb.AppendLine();

    sb.AppendLine("🧾 *Despesas do salário acumulado no período:*");

    if (!salaryExpensesOrdered.Any())
    {
        sb.AppendLine("Nenhuma despesa registrada a partir do salário acumulado neste período.");
    }
    else
    {
        foreach (var tx in salaryExpensesOrdered)
        {
            var dateLocal = tx.TransactionDate.ToLocalTime();
            var desc = string.IsNullOrWhiteSpace(tx.Description)
                ? "Sem descrição"
                : tx.Description;

            // Formato pedido: Despesa X | Data
            sb.AppendLine($"- R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($"📌 *Total de saídas do salário acumulado:* R$ {totalExternalExpenses:N2}");
    }

    sb.AppendLine();

    // -------- CAIXINHAS (detalhadas) --------
    sb.AppendLine("📦 *Caixinhas – despesas no período*");

    if (!ccExpenseGroups.Any())
    {
        sb.AppendLine("Nenhuma despesa registrada em caixinhas neste período.");
    }
    else
    {
        foreach (var group in ccExpenseGroups.OrderBy(g => g.Key))
        {
            var centerId = group.Key;
            var center = allCenters.FirstOrDefault(c => c.Id == centerId);
            var centerName = center?.Name ?? $"Caixinha Id {centerId}";

            if (center != null && center.Type == CostCenterType.InfraMensal)
                centerName += " (Infra Mensal)";

            var totalCenter = group.Sum(x => x.Amount);

            // Cabeçalho da caixinha
            sb.AppendLine();
            sb.AppendLine($"📦 *{centerName}*");
            sb.AppendLine($"Total de despesas: R$ {totalCenter:N2}");

            // Detalhe das despesas da caixinha
            foreach (var tx in group.OrderBy(x => x.TransactionDate))
            {
                var dateLocal = tx.TransactionDate.ToLocalTime();
                var desc = string.IsNullOrWhiteSpace(tx.Description)
                    ? "Sem descrição"
                    : tx.Description;

                sb.AppendLine($"- R$ {tx.Amount:N2} | {desc} | {dateLocal:dd/MM/yyyy}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"📦 *Total de despesas em caixinhas:* R$ {totalCcExpenses:N2}");
    }

    await _telegramSender.SendMessageAsync(
        chatId,
        sb.ToString(),
        cancellationToken);
}
}