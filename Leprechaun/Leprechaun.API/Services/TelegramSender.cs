namespace Leprechaun.API.Services;

public class TelegramSender : ITelegramSender
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    
    
    public TelegramSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Vamos ler o token da config (depois colocamos em appsettings ou variável de ambiente)
        _botToken = configuration["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Telegram:BotToken not configured.");
    }

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = text
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}