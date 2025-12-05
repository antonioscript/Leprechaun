using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.Patrimony;

public class PatrimonyFlowService : IChatFlow
{
    private readonly IFinanceTransactionService _transactionService;
    private readonly ICostCenterService _costCenterService;
    private readonly IPersonService _personService;
    private readonly ITelegramSender _telegramSender;

    public PatrimonyFlowService(
        IFinanceTransactionService transactionService,
        ICostCenterService costCenterService,
        IPersonService personService,
        ITelegramSender telegramSender)
    {
        _transactionService = transactionService;
        _costCenterService = costCenterService;
        _personService = personService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (command != TelegramCommand.Patrimonio)
            return false;

        await HandlePatrimonyAsync(chatId, cancellationToken);
        return true;
    }

    private async Task HandlePatrimonyAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        // --- 1) Salário acumulado total ---
        var totalSalaryAccumulated =
            await _transactionService.GetTotalSalaryAccumulatedAsync(cancellationToken);

        // --- 2) Saldo por titular ---
        var persons = await _personService.GetAllAsync(cancellationToken);
        var salaryPerPerson = new List<(string PersonName, decimal Balance)>();

        foreach (var person in persons)
        {
            var balance = await _transactionService
                .GetSalaryAccumulatedAsync(person.Id, cancellationToken);

            salaryPerPerson.Add((person.Name, balance));
        }

        // --- 3) Caixinhas ---
        var costCenters = await _costCenterService.GetAllAsync(cancellationToken);

        var costCenterBalances = new List<(string Name, decimal Balance, string Owner)>();
        decimal totalCostCenters = 0m;

        foreach (var cc in costCenters)
        {
            var balance = await _transactionService
                .GetCostCenterBalanceAsync(cc.Id, cancellationToken);

            var ownerName = persons.FirstOrDefault(p => p.Id == cc.PersonId)?.Name ?? "Desconhecido";

            costCenterBalances.Add((cc.Name, balance, ownerName));
            totalCostCenters += balance;
        }

        // --- 4) Patrimônio total ---
        var totalPatrimony = totalSalaryAccumulated + totalCostCenters;

        var sb = new StringBuilder();
        sb.AppendLine("🏦 Visão geral do patrimônio atual");
        sb.AppendLine();

        // --- Salário acumulado total ---
        sb.AppendLine($"💰 Salário acumulado total: R$ {totalSalaryAccumulated:N2}");
        sb.AppendLine();

        // --- Salário por titular ---
        sb.AppendLine("👤 Salário acumulado por titular:");
        foreach (var (name, balance) in salaryPerPerson)
            sb.AppendLine($"- {name}: R$ {balance:N2}");
            sb.AppendLine($"- {name}: R$ {balance:N2}");

        sb.AppendLine();

        // --- Caixinhas ---
        if (costCenterBalances.Count == 0)
        {
            sb.AppendLine("📦 Caixinhas:\n");
            sb.AppendLine("Nenhuma caixinha cadastrada ainda.");
        }
        else
        {
            sb.AppendLine("📦 Caixinhas:");
            sb.AppendLine();

            foreach (var (name, balance, owner) in costCenterBalances)
            {
                sb.AppendLine($"- {name} ({owner}): R$ {balance:N2}");
            }

            sb.AppendLine();
            sb.AppendLine($"📦 Total em caixinhas: R$ {totalCostCenters:N2}");
        }

        sb.AppendLine();
        sb.AppendLine("📊 Patrimônio total (salário + caixinhas):");
        sb.AppendLine($"➡️ R$ {totalPatrimony:N2}");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }
}
