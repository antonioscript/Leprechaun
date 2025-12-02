using System.Net.Http.Json;
using Leprechaun.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Leprechaun.Application.Services;

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
        Console.WriteLine($"[TelegramSender] chatId = {chatId}, text = '{text}'");

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = text,
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"[TelegramSender] StatusCode = {(int)response.StatusCode}, Body = {responseBody}");

        response.EnsureSuccessStatusCode();
    }
}