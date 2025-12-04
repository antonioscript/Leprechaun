using System.Globalization;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.TransferSalaryToCostCenter;

public class TransferSalaryToCostCenterFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public TransferSalaryToCostCenterFlowService(
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
        if (state.State == FlowStates.SalaryToCostCenterAwaitingPerson ||
            state.State == FlowStates.SalaryToCostCenterAwaitingTarget ||
            state.State == FlowStates.SalaryToCostCenterAwaitingAmount)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        if (command == TelegramCommand.TransferirSalAcmlParaCaixinha)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.SalaryToCostCenterAwaitingPerson;
        state.TempPersonId = null;
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

        var text = "👤 Selecione o *titular* da transferência do salário acumulado para caixinha:";

        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        switch (state.State)
        {
            case FlowStates.SalaryToCostCenterAwaitingPerson:
                await HandlePersonAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.SalaryToCostCenterAwaitingTarget:
                await HandleTargetAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.SalaryToCostCenterAwaitingAmount:
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
        state.State = FlowStates.SalaryToCostCenterAwaitingTarget;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var costCenters = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId)
            .ToList();

        if (!costCenters.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Esse titular não possui caixinhas para receber a transferência.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = $"📦 Selecione a *caixinha de destino* do titular {person.Name}:";

        var buttons = costCenters
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

        if (state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: titular não definido. Recomece com /transferir_sal_acml_para_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        state.TempTargetCostCenterId = targetId;
        state.State = FlowStates.SalaryToCostCenterAwaitingAmount;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "💰 Informe o valor a transferir do salário acumulado para a caixinha. Ex: 2500,00",
            cancellationToken);
    }

    private async Task HandleAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null || state.TempTargetCostCenterId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: dados incompletos. Recomece com /transferir_sal_acml_para_caixinha.",
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
        var targetId = state.TempTargetCostCenterId.Value;

        await _transactionService.TransferFromSalaryToCostCenterAsync(
            personId,
            targetId,
            amount,
            DateTime.UtcNow,
            "Transferência do salário acumulado para caixinha via bot",
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "✅ Transferência do salário acumulado para caixinha registrada com sucesso!",
            cancellationToken);
    }
}
