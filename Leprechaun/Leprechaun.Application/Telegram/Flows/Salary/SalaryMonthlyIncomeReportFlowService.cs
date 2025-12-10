using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.Salary;

public class SalaryMonthlyIncomeReportFlowService : IChatFlow
{
    private readonly IFinanceTransactionService _transactionService;
    private readonly IInstitutionService _institutionService;
    private readonly ITelegramSender _telegramSender;

    public SalaryMonthlyIncomeReportFlowService(
        IFinanceTransactionService transactionService,
        IInstitutionService institutionService,
        ITelegramSender telegramSender)
    {
        _transactionService = transactionService;
        _institutionService = institutionService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        // Fluxo simples: só reage ao comando
        if (command != TelegramCommand.SaldoSalarios)
            return false;

        await HandleReportAsync(chatId, cancellationToken);
        return true;
    }

    private async Task HandleReportAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(
            now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Todas as transações
        var allTx = await _transactionService.GetAllAsync(cancellationToken);

        // Aqui estou assumindo que TransactionType = "Income" significa recebimento de salário.
        // Se na sua implementação for "SalaryIncome" ou algo assim, é só ajustar o string.
        var monthSalaries = allTx
            .Where(t =>
                t.TransactionType == "Income" &&
                t.TransactionDate >= startOfMonth &&
                t.TransactionDate <= now)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        if (!monthSalaries.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "💵 Nenhum recebimento de salário registrado neste mês até o momento.",
                cancellationToken);
            return;
        }

        // Carrega instituições pra mostrar o nome bonitinho
        var institutions = (await _institutionService.GetAllAsync(cancellationToken))
            .ToDictionary(i => i.Id, i => i.Name);

        var sb = new StringBuilder();
        sb.AppendLine("💵 Recebimentos de salários no mês atual");
        sb.AppendLine();

        decimal total = 0m;

        foreach (var tx in monthSalaries)
        {
            total += tx.Amount;

            var dateLocal = tx.TransactionDate.ToLocalTime();

            string instName = "Instituição desconhecida";
            if (tx.InstitutionId.HasValue &&
                institutions.TryGetValue(tx.InstitutionId.Value, out var name))
            {
                instName = name;
            }

            sb.AppendLine(
                $"- R$ {tx.Amount:N2} | {instName} | {dateLocal:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($"📌 Total recebido no mês: R$ {total:N2}");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }
}
