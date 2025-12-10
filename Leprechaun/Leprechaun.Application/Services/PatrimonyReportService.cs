using System.Text;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Services;

public class PatrimonyReportService : IPatrimonyReportService
{
    private readonly IFinanceTransactionService _transactionService;
    private readonly ICostCenterService _costCenterService;
    private readonly IPersonService _personService;

    public PatrimonyReportService(
        IFinanceTransactionService transactionService,
        ICostCenterService costCenterService,
        IPersonService personService)
    {
        _transactionService = transactionService;
        _costCenterService = costCenterService;
        _personService = personService;
    }

    // ========= TEXTO (TELEGRAM) =========
    public async Task<string> BuildPatrimonyReportAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var data = await BuildPatrimonyReportDataAsync(start, end, cancellationToken);

        var sb = new StringBuilder();

        sb.AppendLine("*Relatório de Patrimônio*");
        sb.AppendLine($"Data: de {start:dd/MM/yyyy} até {end:dd/MM/yyyy}");
        sb.AppendLine();

        sb.AppendLine("*Visão geral (patrimônio)*");
        sb.AppendLine($"Entradas: R$ {data.GeneralEntries:N2}");
        sb.AppendLine($"Saídas (despesas externas, sem caixinhas): R$ {data.GeneralOutflows:N2}");
        sb.AppendLine($"Saldo: R$ {data.GeneralBalance:N2}");
        sb.AppendLine();

        sb.AppendLine("*Salário acumulado*");
        sb.AppendLine($"Entradas: R$ {data.SalaryEntries:N2}");
        sb.AppendLine($"Saídas (apenas despesas externas): R$ {data.SalaryOutflows:N2}");
        sb.AppendLine();

        sb.AppendLine("*Despesas do salário acumulado no período:*");
        foreach (var exp in data.SalaryExpenses.OrderBy(e => e.Date))
        {
            sb.AppendLine($"- R$ {exp.Amount:N2} | {exp.Description} | {exp.Date:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($"*Total de saídas do salário acumulado:* R$ {data.SalaryOutflows:N2}");
        sb.AppendLine();

        sb.AppendLine("*Caixinhas – despesas no período*");
        sb.AppendLine();

        foreach (var cc in data.CostCenters)
        {
            sb.AppendLine($"📦 *{cc.Name}*");
            sb.AppendLine($"Total de despesas: R$ {cc.TotalExpenses:N2}");

            foreach (var exp in cc.Expenses.OrderBy(e => e.Date))
            {
                sb.AppendLine($"- R$ {exp.Amount:N2} | {exp.Description} | {exp.Date:dd/MM/yyyy}");
            }

            sb.AppendLine();
        }

        var totalExpensesInCenters = data.CostCenters.Sum(c => c.TotalExpenses);
        sb.AppendLine($"📦 *Total de despesas em caixinhas:* R$ {totalExpensesInCenters:N2}");

        return sb.ToString();
    }

    // ========= DADOS (PDF) =========
    public async Task<PatrimonyReportDto> BuildPatrimonyReportDataAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var result = new PatrimonyReportDto();

        var allTransactions = await _transactionService.GetAllAsync(cancellationToken);

        var periodTx = allTransactions
            .Where(t => t.TransactionDate >= start && t.TransactionDate <= end)
            .ToList();

        // ---- Visão geral ----
        var entries = periodTx
            .Where(t => t.TransactionType == "Income")
            .ToList();

        var externalOutflows = periodTx
            .Where(t => t.TransactionType == "Expense" &&
                        t.SourceCostCenterId == null)
            .ToList();

        result.GeneralEntries = entries.Sum(t => t.Amount);
        result.GeneralOutflows = externalOutflows.Sum(t => t.Amount);
        result.GeneralBalance = result.GeneralEntries - result.GeneralOutflows;

        // ---- Salário acumulado ----
        // (aqui estou assumindo que "salário acumulado" = entradas – despesas externas;
        // se você tiver uma lógica diferente, ajusta só esses filtros)
        result.SalaryEntries = result.GeneralEntries;
        result.SalaryOutflows = result.GeneralOutflows;
        result.SalaryExpenses = externalOutflows
            .Select(t => new ExpenseDto
            {
                Date = t.TransactionDate,
                Description = string.IsNullOrWhiteSpace(t.Description)
                    ? "Sem descrição"
                    : t.Description,
                Amount = t.Amount
            })
            .OrderBy(e => e.Date)
            .ToList();

        // ---- Caixinhas ----
        var centers = (await _costCenterService.GetAllAsync(cancellationToken)).ToList();
        var centersById = centers.ToDictionary(c => c.Id, c => c.Name);

        var expensesInCenters = periodTx
            .Where(t => t.TransactionType == "Expense" &&
                        t.SourceCostCenterId != null)
            .ToList();

        var byCenter = expensesInCenters
            .GroupBy(t => t.SourceCostCenterId!.Value);

        foreach (var group in byCenter)
        {
            var centerId = group.Key;
            centersById.TryGetValue(centerId, out var centerName);
            centerName ??= $"Caixinha {centerId}";

            var ccDto = new CostCenterReportDto
            {
                CostCenterId = centerId,
                Name = centerName,
                TotalExpenses = group.Sum(t => t.Amount),
                Expenses = group
                    .OrderBy(t => t.TransactionDate)
                    .Select(t => new ExpenseDto
                    {
                        Date = t.TransactionDate,
                        Description = string.IsNullOrWhiteSpace(t.Description)
                            ? "Sem descrição"
                            : t.Description,
                        Amount = t.Amount
                    })
                    .ToList()
            };

            result.CostCenters.Add(ccDto);
        }

        return result;
    }
}