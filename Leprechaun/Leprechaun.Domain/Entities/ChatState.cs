namespace Leprechaun.Domain.Entities;

public class ChatState
{
    public long ChatId { get; set; }

    public string State { get; set; } = "Idle";

    public int? TempInstitutionId { get; set; }

    public decimal? TempAmount { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}