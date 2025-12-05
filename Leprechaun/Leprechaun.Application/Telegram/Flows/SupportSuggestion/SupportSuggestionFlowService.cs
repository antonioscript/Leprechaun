// Leprechaun.Application/Telegram/Flows/SupportSuggestion/SupportSuggestionFlowService.cs
using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.SupportSuggestion;

public class SupportSuggestionFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly ISupportSuggestionService _supportSuggestionService;
    private readonly ITelegramSender _telegramSender;

    public SupportSuggestionFlowService(
        IChatStateService chatStateService,
        ISupportSuggestionService supportSuggestionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _supportSuggestionService = supportSuggestionService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        // 1) Continuação do fluxo de sugestão
        if (state.State == FlowStates.SupportSuggestionAwaitingDescription)
        {
            await HandleDescriptionAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // 2) Início do fluxo /sugerir_feature
        if (command == TelegramCommand.SugerirFeature)
        {
            await StartSuggestionFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        // 3) Listagem /listar_features (sem estado)
        if (command == TelegramCommand.ListarFeatures)
        {
            await HandleListAsync(chatId, cancellationToken);
            return true;
        }

        // 4) Não é responsabilidade deste fluxo
        return false;
    }

    // ============================
    //  /sugerir_feature
    // ============================
    private async Task StartSuggestionFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.SupportSuggestionAwaitingDescription;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("💡 Sugestão de melhoria / nova feature");
        sb.AppendLine();
        sb.AppendLine("Escreva sua sugestão em uma mensagem única.");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }

    private async Task HandleDescriptionAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        var description = userText?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ A descrição da sugestão não pode ser vazia. Tente novamente.",
                cancellationToken);
            return;
        }

        // Salva a sugestão e pega o Id
        var suggestion = await _supportSuggestionService.CreateAsync(
            chatId,
            description,
            cancellationToken);

        // Limpa o estado do fluxo
        await _chatStateService.ClearAsync(chatId, cancellationToken);

        // Mensagem de agradecimento / próximos passos
        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.HintAfterSuggestion(suggestion.Id),
            cancellationToken);
    }

    // ============================
    //  /listar_features
    // ============================
    private async Task HandleListAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        var suggestions = await _supportSuggestionService.GetAllAsync(cancellationToken);

        if (suggestions == null || suggestions.Count == 0)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                BotTexts.NoSuggestions(),
                cancellationToken);
            return;
        }

        // Vamos mostrar, por exemplo, as últimas 10
        var latest = suggestions
            .OrderByDescending(s => s.CreatedAt)
            .Take(10)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(BotTexts.FormatSuggestionListHeader());
        sb.AppendLine();

        foreach (var s in latest)
        {
            var dateLocal = s.CreatedAt.ToLocalTime();

            sb.AppendLine(
                $"• #{s.Id} | {s.Status} | {dateLocal:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"  {s.Description}");
            sb.AppendLine();
        }

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }
}
