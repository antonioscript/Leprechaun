using Leprechaun.Domain.Enums;

namespace Leprechaun.Domain.Entities;

public class CostCenter
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int PersonId { get; set; }
    public Person? Person { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public CostCenterType Type { get; set; } = CostCenterType.Default;

}