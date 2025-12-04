// Leprechaun.Application/Telegram/Flows/SalaryInfo/SalaryAccumulatedInfoFlowService.cs
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using System.Text;

namespace Leprechaun.Application.Telegram.Flows.SalaryAccumulatedInfo;

public class SalaryAccumulatedInfoFlowService : IChatFlow
{
    private readonly IFinanceTransactionService _transactionService;
    private readonly IPersonService _personService;
    private readonly ITelegramSender _telegramSender;

    public SalaryAccumulatedInfoFlowService(
        IFinanceTransactionService transactionService,
        IPersonService personService,
        ITelegramSender telegramSender)
    {
        _transactionService = transactionService;
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
        // Só responde ao comando /saldo_salario_acumulado
        if (command != TelegramCommand.SaldoSalarioAcumulado)
            return false;

        // 1) Saldo total (todos)
        var total = await _transactionService
            .GetTotalSalaryAccumulatedAsync(cancellationToken);

        // 2) Buscar pessoas (Antônio e Catarina)
        var persons = await _personService.GetAllAsync(cancellationToken);

        var catarina = persons
            .FirstOrDefault(p =>
                p.Name.Equals("Catarina", StringComparison.OrdinalIgnoreCase));

        var antonio = persons
            .FirstOrDefault(p =>
                p.Name.Equals("Antonio", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("Antônio", StringComparison.OrdinalIgnoreCase));

        decimal catarinaAmount = 0m;
        decimal antonioAmount = 0m;

        if (catarina is not null)
            catarinaAmount = await _transactionService
                .GetSalaryAccumulatedAsync(catarina.Id, cancellationToken);

        if (antonio is not null)
            antonioAmount = await _transactionService
                .GetSalaryAccumulatedAsync(antonio.Id, cancellationToken);

        // 3) Última atualização
        var lastUpdate = await _transactionService
            .GetLastSalaryAccumulatedUpdateAsync(cancellationToken);

        // 4) Percentual de 1 milhão
        const decimal target = 1_000_000m;
        var percentOfMillion = target > 0m
            ? (total / target) * 100m
            : 0m;

        var sb = new StringBuilder();

        sb.AppendLine("*💼 Saldo do Salário Acumulado*");
        sb.AppendLine();
        sb.AppendLine($"💰 Saldo total: R$ {total:N2}");
        sb.AppendLine();

        sb.AppendLine($"👩 Catarina: R$ {catarinaAmount:N2}");
        sb.AppendLine($"👨 Antônio: R$ {antonioAmount:N2}");
        sb.AppendLine();

        if (lastUpdate.HasValue)
            sb.AppendLine($"🕒 Última atualização: {lastUpdate:dd/MM/yyyy HH:mm}");
        else
            sb.AppendLine("🕒 Última atualização: Sem movimentações registradas.");

        sb.AppendLine();
        sb.AppendLine($"📊 Isso representa {percentOfMillion:N2}% de R$ 1.000.000,00.");

        var text = sb.ToString();

        await _telegramSender.SendMessageAsync(
            chatId,
            text,
            cancellationToken);

        return true;
    }
}
