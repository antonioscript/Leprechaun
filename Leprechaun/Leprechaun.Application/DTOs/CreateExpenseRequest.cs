using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leprechaun.Application.DTOs;

public class CreateExpenseRequest
{
    public int CostCenterId { get; set; }     // ← obrigatório na criação

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public decimal? DefaultAmount { get; set; }
    public short? DueDay { get; set; }

    public int? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
