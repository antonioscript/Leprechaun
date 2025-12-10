using System.Text;
using Leprechaun.Domain.Interfaces;

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

    public async Task<string> BuildPatrimonyReportAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        // ⚠️ Aqui você pega o StringBuilder que já montamos no fluxo
        // /relatorio_patrimonio (Entradas, Saídas, detalhamento por salário acumulado,
        // caixinhas, etc.) e move o código para cá.
        //
        // Vou deixar um esqueleto, mas a ideia é literalmente copiar a lógica
        // existente da classe de fluxo e colar aqui.

        var sb = new StringBuilder();

        sb.AppendLine("📊 Relatório de Patrimônio");
        sb.AppendLine($"📅 Período: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}");
        sb.AppendLine();

        // --- EXEMPLO SUPER SIMPLIFICADO (troque pelo seu de verdade) ---

        var all = await _transactionService.GetAllAsync(cancellationToken);

        var entries = all
            .Where(t => t.TransactionType == "Income"
                        && t.TransactionDate >= start
                        && t.TransactionDate <= end)
            .ToList();

        var externalOutflows = all
            .Where(t => t.TransactionType == "Expense"
                        && t.TransactionDate >= start
                        && t.TransactionDate <= end
                        && t.SourceCostCenterId == null) // só saídas externas
            .ToList();

        var totalEntries = entries.Sum(t => t.Amount);
        var totalExternalOutflows = externalOutflows.Sum(t => t.Amount);
        var saldo = totalEntries - totalExternalOutflows;

        sb.AppendLine("💵 Movimentação geral:");
        sb.AppendLine($"➡️ Entradas: R$ {totalEntries:N2}");
        sb.AppendLine($"⬅️ Saídas externas: R$ {totalExternalOutflows:N2}");
        sb.AppendLine($"💼 Saldo (Entradas - Saídas externas): R$ {saldo:N2}");
        sb.AppendLine();

        // Aqui você inclui TODO o restante do relatório que já tínhamos:
        // - Seção “SALÁRIO ACUMULADO” com entradas/saídas + lista de despesas
        // - Seções por caixinha, com despesas listadas
        // etc.

        return sb.ToString();
    }
}