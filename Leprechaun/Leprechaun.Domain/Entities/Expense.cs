using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace Leprechaun.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CostCenterId { get; set; }

    [JsonIgnore]                  // não entra no JSON
    [ValidateNever]
    public CostCenter CostCenter { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public decimal? DefaultAmount { get; set; }
    public short? DueDay { get; set; }

    public int? CategoryId { get; set; }

    [JsonIgnore]
    [ValidateNever]
    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;
}
