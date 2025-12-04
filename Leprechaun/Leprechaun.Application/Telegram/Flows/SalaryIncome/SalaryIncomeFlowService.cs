using System.Globalization;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.SalaryIncome;

public class SalaryIncomeFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IInstitutionService _institutionService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public SalaryIncomeFlowService(
        IChatStateService chatStateService,
        IInstitutionService institutionService,
        IFinanceTransactionService transactionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _institutionService = institutionService;
        _transactionService = transactionService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (state.State == FlowStates.SalaryAwaitingInstitution ||
            state.State == FlowStates.SalaryAwaitingAmount)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        if (command == TelegramCommand.CadastrarSalario)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    // ----------- Início do fluxo -----------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        var institutions = (await _institutionService.GetAllAsync(cancellationToken))
            .Where(i => i.IsActive)
            .ToList();

        if (!institutions.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Não há instituições cadastradas.",
                cancellationToken);
            return;
        }

        // Texto da pergunta
        var text = "🏦 *Escolha a instituição do salário:*";

        // Inline buttons – aqui eu uso o ÍNDICE como callback_data
        // para reaproveitar sua lógica atual que espera "1", "2", "3"...
        var buttons = institutions
            .Select((inst, index) =>
                (Label: inst.Name, Data: (index + 1).ToString()))
            .ToList();

        state.State = FlowStates.SalaryAwaitingInstitution;
        state.TempInstitutionId = null;
        state.TempAmount = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    // ----------- Continuação do fluxo (igual você já fez) -----------

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.State == FlowStates.SalaryAwaitingInstitution)
        {
            await HandleChooseInstitutionAsync(chatId, userText, state, cancellationToken);
        }
        else if (state.State == FlowStates.SalaryAwaitingAmount)
        {
            await HandleAmountAsync(chatId, userText, state, cancellationToken);
        }
    }

    private async Task HandleChooseInstitutionAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var index))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Envie um número válido para escolher a instituição.",
                cancellationToken);
            return;
        }

        var institutions = (await _institutionService.GetAllAsync(cancellationToken))
            .Where(i => i.IsActive)
            .ToList();

        if (index < 1 || index > institutions.Count)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Número inválido. Tente novamente.",
                cancellationToken);
            return;
        }

        var chosen = institutions[index - 1];

        state.TempInstitutionId = chosen.Id;
        state.State = FlowStates.SalaryAwaitingAmount;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            TextsSalaryIncome.AskSalaryAmount(chosen.Name),
            cancellationToken);
    }

    private async Task HandleAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        var normalized = userText.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        normalized = normalized.Replace(".", "").Replace(",", ".");

        if (!decimal.TryParse(
                normalized,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Valor inválido. Tente novamente. Ex: 2500",
                cancellationToken);
            return;
        }

        if (state.TempInstitutionId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Erro interno: instituição temporária não encontrada. Reinicie o fluxo com /cadastrar_salario.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var institution = await _institutionService.GetByIdAsync(
            state.TempInstitutionId.Value,
            cancellationToken);

        if (institution is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Erro interno: instituição não encontrada. Reinicie o fluxo com /cadastrar_salario.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        await _transactionService.RegisterIncomeAsync(
            personId: institution.PersonId,
            institutionId: institution.Id,
            amount: amount,
            date: DateTime.UtcNow,
            targetCostCenterId: null,
            categoryId: null,
            description: "Salário cadastrado via bot",
            cancellationToken: cancellationToken
        );

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        var total = await _transactionService.GetTotalSalaryAccumulatedAsync(cancellationToken);

        var reply = TextsSalaryIncome.SalaryReceipt(
            institution,
            amount,
            DateTime.Now,
            total);

        await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
    }
}
