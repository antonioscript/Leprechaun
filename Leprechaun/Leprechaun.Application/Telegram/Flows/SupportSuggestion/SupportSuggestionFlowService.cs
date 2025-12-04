// Leprechaun.Application/Telegram/Flows/SupportSuggestion/SupportSuggestionFlowService.cs
using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.SupportSuggestion;

public class SupportSuggestionFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly ISupportSuggestionService _suggestionService;
    private readonly ITelegramSender _telegramSender;

    public SupportSuggestionFlowService(
        IChatStateService chatStateService,
        ISupportSuggestionService suggestionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _suggestionService = suggestionService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        // Continuação do fluxo de sugestão
        if (state.State == FlowStates.SupportSuggestionAwaitingDescription)
        {
            await HandleDescriptionAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // Inicia fluxo de sugestão
        if (command == TelegramCommand.SugerirFeature)
        {
            await StartSuggestionFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        // Fluxo de LISTAR sugestões (stateless)
        if (command == TelegramCommand.ListarFeatures)
        {
            await ShowSuggestionsAsync(chatId, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task StartSuggestionFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.SupportSuggestionAwaitingDescription;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "🛟 Suporte / Ideias\n\n" +
            "Me conta a sua sugestão, dúvida ou problema.\n\n" +
            "Exemplos:\n" +
            "- Queria um relatório mensal separado por titular\n" +
            "- Seria legal poder arquivar uma caixinha\n",
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
                "⚠️ A descrição não pode ser vazia. Me conta com algumas palavras o que você gostaria:",
                cancellationToken);
            return;
        }

        var suggestion = await _suggestionService.CreateAsync(
            chatId,
            description,
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.HintAfterSuggestion(suggestion.Id),
            cancellationToken);
    }

    private async Task ShowSuggestionsAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        var list = await _suggestionService.GetAllAsync(cancellationToken);

        if (list.Count == 0)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                BotTexts.NoSuggestions(),
                cancellationToken);
            return;
        }

        var top = list.Take(10).ToList();

        var sb = new StringBuilder();
        sb.AppendLine(BotTexts.FormatSuggestionListHeader());
        sb.AppendLine();

        foreach (var s in top)
        {
            var shortDesc = s.Description;
            if (shortDesc.Length > 80)
                shortDesc = shortDesc.Substring(0, 77) + "...";

            sb.AppendLine(
                $"#{s.Id} - {s.CreatedAt:dd/MM/yyyy HH:mm} - {shortDesc}");
        }

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }
}
