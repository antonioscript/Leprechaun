namespace Leprechaun.Domain.Entities;

public class Institution
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!; // CLT, Bank, PJ, Donation, etc.
    public int PersonId { get; set; }
    public Person? Person { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}