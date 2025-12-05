using System.Text;
using Leprechaun.Application.Services;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.SalaryStatement;

public class SalaryAccumulatedMonthlyStatementFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;
    private readonly ICostCenterService _costCenterService;

    public SalaryAccumulatedMonthlyStatementFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        IFinanceTransactionService transactionService,
        ITelegramSender telegramSender,
        ICostCenterService costCenterService)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _transactionService = transactionService;
        _telegramSender = telegramSender;
        _costCenterService = costCenterService;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (command != TelegramCommand.ExtratoSalarioAcumuladoMes)
            return false;

        await HandleStatementAsync(chatId, cancellationToken);
        return true;
    }

    private async Task HandleStatementAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        // Saldo total atual (todos os titulares)
        var totalAccumulated = await _transactionService
            .GetTotalSalaryAccumulatedAsync(cancellationToken);

        // Carrega tudo para filtrar
        var allTx = await _transactionService.GetAllAsync(cancellationToken);
        var persons = await _personService.GetAllAsync(cancellationToken);
        var personsById = persons.ToDictionary(p => p.Id, p => p.Name);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Despesas feitas DIRETO do salário acumulado (origem = null)
        var monthExpenses = allTx
            .Where(t =>
                t.TransactionType == "Expense" &&
                t.SourceCostCenterId == null &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        // Transferências internas: salário acumulado -> caixinhas
        var internalTransfers = allTx
            .Where(t =>
                t.TransactionType == "Transfer" &&
                t.SourceCostCenterId == null &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("📊 Extrato do salário acumulado (mês atual)");
        sb.AppendLine($"💼 Saldo atual total (todos os titulares): R$ {totalAccumulated:N2}");
        sb.AppendLine();

        // ---------- DESPESAS DIRETAS ----------

        if (!monthExpenses.Any())
        {
            sb.AppendLine("🧾 *espesas diretas no mês:\n");
            sb.AppendLine("Nenhuma despesa registrada a partir do salário acumulado neste mês.");
        }
        else
        {
            sb.AppendLine("🧾 Despesas diretas no mês:\n");

            foreach (var tx in monthExpenses)
            {
                var desc = string.IsNullOrWhiteSpace(tx.Description)
                    ? "Sem descrição"
                    : tx.Description;

                var personName = personsById.TryGetValue(tx.PersonId, out var pName)
                    ? pName
                    : "Desconhecido";

                var dateLocal = tx.TransactionDate.ToLocalTime();

                sb.AppendLine(
                    $"- R$ {tx.Amount:N2} | {desc} | {personName} | {dateLocal:dd/MM/yyyy}");
                sb.AppendLine();
            }

            var totalExpenses = monthExpenses.Sum(t => t.Amount);
            sb.AppendLine();
            sb.AppendLine($"💸 Total de despesas diretas no mês: R$ {totalExpenses:N2}");
        }

        // ---------- TRANSFERÊNCIAS INTERNAS ----------
        var costCenters = await _costCenterService.GetAllAsync(cancellationToken);
        var costCenterNames = costCenters.ToDictionary(c => c.Id, c => c.Name);

        if (internalTransfers.Any())
        {
            sb.AppendLine();
            sb.AppendLine("🔁 Transferências internas no mês (salário → caixinhas):");
            sb.AppendLine();

            foreach (var tx in internalTransfers)
            {
                var personName = personsById.TryGetValue(tx.PersonId, out var pName)
                    ? pName
                    : "Desconhecido";

                var dateLocal = tx.TransactionDate.ToLocalTime();

                // Aqui trocamos o ID pelo nome da caixinha ❤️
                string targetName = "caixinha";
                if (tx.TargetCostCenterId is int ccId &&
                    costCenterNames.TryGetValue(ccId, out var ccName))
                {
                    targetName = $"caixinha *{ccName}*";
                }

                sb.AppendLine(
                    $"- R$ {tx.Amount:N2} | Transferência para {targetName} | {personName} | {dateLocal:dd/MM/yyyy}");
                sb.AppendLine();
            }

            var totalTransfers = internalTransfers.Sum(t => t.Amount);
            var totalExpenses = monthExpenses.Sum(t => t.Amount);
            var totalOut = totalExpenses + totalTransfers;

            sb.AppendLine();
            sb.AppendLine($"💸 Total de despesas diretas no mês: R$ {totalExpenses:N2}");
            sb.AppendLine($"🔼 Total transferido para caixinhas no mês: R$ {totalTransfers:N2}");
            sb.AppendLine();
            sb.AppendLine($"📉 Total de saídas do salário acumulado (despesas + transferências): R$ {totalOut:N2}");
        }


        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }
}
