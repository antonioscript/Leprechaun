// Leprechaun.Domain/Entities/SupportSuggestion.cs
namespace Leprechaun.Domain.Entities;

public class SupportSuggestion
{
    public long Id { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public long? ChatId { get; set; }
    public string Source { get; set; } = "Telegram";
    public string Status { get; set; } = "Open";
}
