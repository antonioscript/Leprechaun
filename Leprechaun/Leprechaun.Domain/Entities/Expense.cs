namespace Leprechaun.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CostCenterId { get; set; }
    public CostCenter CostCenter { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public decimal? DefaultAmount { get; set; }
    public short? DueDay { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;
}
