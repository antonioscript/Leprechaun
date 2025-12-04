using System.Globalization;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.TransferBetweenCostCenters;

public class TransferBetweenCostCentersFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public TransferBetweenCostCentersFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        ICostCenterService costCenterService,
        IFinanceTransactionService transactionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _costCenterService = costCenterService;
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
        // Já está dentro do fluxo?
        if (state.State == FlowStates.CostCenterTransferAwaitingPerson ||
            state.State == FlowStates.CostCenterTransferAwaitingSource ||
            state.State == FlowStates.CostCenterTransferAwaitingTarget ||
            state.State == FlowStates.CostCenterTransferAwaitingAmount)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // Comando para iniciar o fluxo
        if (command == TelegramCommand.TransferirEntreCaixinhas)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    // ---------- Início do fluxo ----------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.CostCenterTransferAwaitingPerson;
        state.TempPersonId = null;
        state.TempSourceCostCenterId = null;
        state.TempTargetCostCenterId = null;
        state.TempAmount = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var persons = (await _personService.GetAllAsync(cancellationToken)).ToList();
        if (!persons.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não há titulares cadastrados para realizar a transferência.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = "👤 Selecione o *titular* da transferência entre caixinhas:";

        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    // ---------- Continuação do fluxo ----------

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        switch (state.State)
        {
            case FlowStates.CostCenterTransferAwaitingPerson:
                await HandlePersonAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterTransferAwaitingSource:
                await HandleSourceAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterTransferAwaitingTarget:
                await HandleTargetAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterTransferAwaitingAmount:
                await HandleAmountAsync(chatId, userText, state, cancellationToken);
                break;
        }
    }

    private async Task HandlePersonAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var personId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular inválido. Tente clicar novamente no botão.",
                cancellationToken);
            return;
        }

        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault(p => p.Id == personId);
        if (person is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular não encontrado. Tente novamente.",
                cancellationToken);
            return;
        }

        state.TempPersonId = personId;
        state.State = FlowStates.CostCenterTransferAwaitingSource;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var allCostCenters = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId)
            .ToList();

        if (allCostCenters.Count < 2)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Esse titular precisa ter pelo menos *duas* caixinhas para transferir entre elas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = $"📦 Selecione a *caixinha de origem* do titular {person.Name}:";

        var buttons = allCostCenters
            .Select(c => (Label: c.Name, Data: c.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    private async Task HandleSourceAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var sourceId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Caixinha de origem inválida. Tente clicar novamente no botão.",
                cancellationToken);
            return;
        }

        if (state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: titular não definido. Recomece com /transferir_entre_caixinhas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        state.TempSourceCostCenterId = sourceId;
        state.State = FlowStates.CostCenterTransferAwaitingTarget;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var personId = state.TempPersonId.Value;
        var allCostCenters = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId)
            .ToList();

        var source = allCostCenters.FirstOrDefault(c => c.Id == sourceId);

        var text = $"📦 Selecione a *caixinha de destino* (diferente de \"{source?.Name}\"):";

        var buttons = allCostCenters
            .Where(c => c.Id != sourceId)
            .Select(c => (Label: c.Name, Data: c.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    private async Task HandleTargetAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var targetId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Caixinha de destino inválida. Tente clicar novamente no botão.",
                cancellationToken);
            return;
        }

        if (state.TempSourceCostCenterId is null || state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: origem ou titular não definidos. Recomece com /transferir_entre_caixinhas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        if (state.TempSourceCostCenterId.Value == targetId)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ A caixinha de destino deve ser diferente da caixinha de origem.",
                cancellationToken);
            return;
        }

        state.TempTargetCostCenterId = targetId;
        state.State = FlowStates.CostCenterTransferAwaitingAmount;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "💰 Informe o *valor* a transferir entre as caixinhas. Ex: 2500,00",
            cancellationToken);
    }

    private async Task HandleAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null ||
            state.TempSourceCostCenterId is null ||
            state.TempTargetCostCenterId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: dados incompletos para a transferência. Recomece com /transferir_entre_caixinhas.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var normalized = userText.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        normalized = normalized.Replace(".", "").Replace(",", ".");

        if (!decimal.TryParse(
                normalized,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var amount) || amount <= 0)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Valor inválido. Tente novamente. Ex: 2560,34",
                cancellationToken);
            return;
        }

        var personId = state.TempPersonId.Value;
        var sourceId = state.TempSourceCostCenterId.Value;
        var targetId = state.TempTargetCostCenterId.Value;

        await _transactionService.TransferBetweenCostCentersAsync(
            personId,
            sourceId,
            targetId,
            amount,
            DateTime.UtcNow,
            "Transferência entre caixinhas via bot",
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "✅ Transferência entre caixinhas registrada com sucesso!",
            cancellationToken);
    }
}
