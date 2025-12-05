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
        // 1) Se já estamos no fluxo de sugestão, processar a descrição
        if (state.State == FlowStates.SupportSuggestionAwaitingDescription)
        {
            await HandleDescriptionAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // 2) Se é o comando /sugerir_feature, iniciar o fluxo
        if (command == TelegramCommand.SugerirFeature)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        // 3) Não é com esse fluxo
        return false;
    }

    // ----------- INÍCIO DO FLUXO -----------

    private async Task StartFlowAsync(
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
        sb.AppendLine("Exemplo:");
        sb.AppendLine("_\"Criar um comando para ver o extrato de todas as caixinhas em uma única tela\"_");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }

    // ----------- DESCRIÇÃO DA SUGESTÃO -----------

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

        // Salva a sugestão e recebe o objeto com o Id
        var suggestion = await _supportSuggestionService.CreateAsync(
            chatId,
            description,
            cancellationToken);

        // Limpa o estado do fluxo
        await _chatStateService.ClearAsync(chatId, cancellationToken);

        // Mensagem de agradecimento + dica de próximos passos
        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.HintAfterSuggestion(suggestion.Id),
            cancellationToken);
    }

}
