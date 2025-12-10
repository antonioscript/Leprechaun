namespace Leprechaun.Domain.Response;

public class PatrimonyReportDto
{
    // Visão geral
    public decimal GeneralEntries { get; set; }
    public decimal GeneralOutflows { get; set; }
    public decimal GeneralBalance { get; set; }

    // Salário acumulado
    public decimal SalaryEntries { get; set; }
    public decimal SalaryOutflows { get; set; }
    public List<ExpenseDto> SalaryExpenses { get; set; } = new();

    // Caixinhas
    public List<CostCenterReportDto> CostCenters { get; set; } = new();
}

public class CostCenterReportDto
{
    public int CostCenterId { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal TotalExpenses { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = new();
}

public class ExpenseDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}