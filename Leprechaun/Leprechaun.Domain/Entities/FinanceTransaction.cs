namespace Leprechaun.Domain.Entities;

public class FinanceTransaction
{
    public long Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }

    public int? SourceCostCenterId { get; set; }
    public CostCenter? SourceCostCenter { get; set; }

    public int? TargetCostCenterId { get; set; }
    public CostCenter? TargetCostCenter { get; set; }

    public int? InstitutionId { get; set; }
    public Institution? Institution { get; set; }

    public string TransactionType { get; set; } = null!; // 'Income', 'Expense', 'Transfer', etc.

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Description { get; set; }
}