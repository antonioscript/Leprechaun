using System.Net.Http.Headers;
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
            text = text
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"[TelegramSender] StatusCode = {(int)response.StatusCode}, Body = {responseBody}");

        response.EnsureSuccessStatusCode();
    }

    // NOVO: mensagem com inline keyboard
    public async Task SendMessageWithInlineKeyboardAsync( long chatId, string text, IEnumerable<(string Label, string Data)> buttons, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[TelegramSender] chatId = {chatId}, text = '{text}' [inline keyboard]");

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        // 1 botão por linha (simples)
        var keyboard = buttons
            .Select(b => new[]
            {
                new
                {
                    text = b.Label,
                    callback_data = b.Data
                }
            })
            .ToArray();

        var payload = new
        {
            chat_id = chatId,
            text = text,
            parse_mode = "Markdown",
            reply_markup = new
            {
                inline_keyboard = keyboard
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"[TelegramSender INLINE] StatusCode = {(int)response.StatusCode}, Body = {responseBody}");

        response.EnsureSuccessStatusCode();
    }
    
    
    
    // NOVO: Enviar imagem (foto) a partir de um arquivo local
    public async Task SendPhotoAsync(
        long chatId,
        string filePath,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[TelegramSender] Sending photo to chat {chatId}: {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Arquivo de imagem não encontrado: {filePath}");
        }

        var url = $"https://api.telegram.org/bot{_botToken}/sendPhoto";

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(chatId.ToString()), "chat_id");

        if (!string.IsNullOrWhiteSpace(caption))
            form.Add(new StringContent(caption), "caption");

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var fileContent = new ByteArrayContent(bytes);

        // Deixa o tipo genérico pra não dar conflito com PNG/JPG/etc
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // "photo" é o nome que o Telegram espera
        form.Add(fileContent, "photo", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync(url, form, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        Console.WriteLine($"[TelegramSender PHOTO] StatusCode = {(int)response.StatusCode}, Body = {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            // Joga a mensagem do Telegram na exception pra ficar claro o motivo
            throw new HttpRequestException(
                $"Telegram sendPhoto failed. StatusCode={(int)response.StatusCode}, Body={responseBody}");
        }
    }


}