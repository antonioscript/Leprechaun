namespace Leprechaun.Application.Models;

public class TelegramUpdate
{
    public long UpdateId { get; set; }
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    public long MessageId { get; set; }
    public TelegramChat Chat { get; set; } = default!;
    public string? Text { get; set; }
}

public class TelegramChat
{
    public long Id { get; set; }
    public string? Title { get; set; }
}