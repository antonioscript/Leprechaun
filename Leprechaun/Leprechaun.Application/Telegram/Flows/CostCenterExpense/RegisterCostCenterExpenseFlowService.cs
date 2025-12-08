using System.Globalization;
using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Enums;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.CostCenterExpense;

public class RegisterCostCenterExpenseFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public RegisterCostCenterExpenseFlowService(
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
        if (state.State == FlowStates.CostCenterExpenseAwaitingPerson ||
            state.State == FlowStates.CostCenterExpenseAwaitingCenter ||
            state.State == FlowStates.CostCenterExpenseAwaitingAmount ||
            state.State == FlowStates.CostCenterExpenseAwaitingDescription)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        if (command == TelegramCommand.RegistrarDespesaCaixinha)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        return false;
    }

    // ------------------------- INÍCIO DO FLUXO -------------------------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.CostCenterExpenseAwaitingPerson;
        state.TempPersonId = null;
        state.TempSourceCostCenterId = null;
        state.TempAmount = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var persons = await _personService.GetAllAsync(cancellationToken);
        if (!persons.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não há titulares cadastrados.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            "👤 *Selecione o titular da despesa:*",
            buttons,
            cancellationToken);
    }

    // ------------------------- CONTINUAÇÃO -------------------------

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        switch (state.State)
        {
            case FlowStates.CostCenterExpenseAwaitingPerson:
                await HandlePersonAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterExpenseAwaitingCenter:
                await HandleCenterAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterExpenseAwaitingAmount:
                await HandleAmountAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterExpenseAwaitingDescription:
                await HandleDescriptionAsync(chatId, userText, state, cancellationToken);
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
                "⚠️ Clique em um titular válido.",
                cancellationToken);
            return;
        }

        state.TempPersonId = personId;
        state.State = FlowStates.CostCenterExpenseAwaitingCenter;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var centers = (await _costCenterService.GetAllAsync(cancellationToken))
            .Where(c => c.PersonId == personId &&
                        c.Type != CostCenterType.ProibidaDespesaDireta) // 👈 NÃO MOSTRA PROIBIDA
            .ToList();

        if (!centers.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Esse titular não possui caixinhas elegíveis para despesa.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var buttons = centers
            .Select(c => (Label: c.Name, Data: c.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            "📦 *Selecione a caixinha onde será registrada a despesa:*",
            buttons,
            cancellationToken);
    }

    private async Task HandleCenterAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userText, out var centerId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Clique em uma caixinha válida.",
                cancellationToken);
            return;
        }

        // Segurança extra: verificar se a caixinha não é ProibidaDespesaDireta
        var center = await _costCenterService.GetByIdAsync(centerId, cancellationToken);
        if (center is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Caixinha não encontrada. Tente novamente.",
                cancellationToken);
            return;
        }

        if (center.Type == CostCenterType.ProibidaDespesaDireta)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não é permitido registrar despesa diretamente nessa caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        state.TempSourceCostCenterId = centerId;
        state.State = FlowStates.CostCenterExpenseAwaitingAmount;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var balance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            $"💰 Saldo atual da caixinha: R$ {balance:N2}\n\n" +
            "Informe o valor da despesa:",
            cancellationToken);
    }

    private async Task HandleAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null || state.TempSourceCostCenterId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: dados incompletos. Recomece com /registrar_despesa_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var personId = state.TempPersonId.Value;
        var centerId = state.TempSourceCostCenterId.Value;

        var normalized = userText.Replace("R$", "", StringComparison.OrdinalIgnoreCase)
                                 .Replace(".", "")
                                 .Replace(",", ".")
                                 .Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Valor inválido. Tente novamente (ex: 250,00).",
                cancellationToken);
            return;
        }

        var balance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        if (amount > balance)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                $"⚠️ Saldo insuficiente.\nSaldo atual: *R$ {balance:N2}*",
                cancellationToken);
            return;
        }

        state.TempAmount = amount;
        state.State = FlowStates.CostCenterExpenseAwaitingDescription;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "📝 Informe a descrição da despesa:",
            cancellationToken);
    }

    private async Task HandleDescriptionAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null || state.TempSourceCostCenterId is null || state.TempAmount is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: dados incompletos. Recomece com /registrar_despesa_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var description = userText.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ A descrição não pode ser vazia. Informe um nome para a despesa.",
                cancellationToken);
            return;
        }

        var personId = state.TempPersonId.Value;
        var centerId = state.TempSourceCostCenterId.Value;
        var amount = state.TempAmount.Value;

        await _transactionService.RegisterExpenseFromCostCenterAsync(
            personId,
            centerId,
            amount,
            DateTime.UtcNow,
            categoryId: null,
            description: description,
            cancellationToken);

        var newBalance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.First(p => p.Id == personId);

        var centers = await _costCenterService.GetAllAsync(cancellationToken);
        var center = centers.First(c => c.Id == centerId);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        var reply = new StringBuilder();
        reply.AppendLine("✅ Despesa registrada com sucesso!");
        reply.AppendLine();
        reply.AppendLine($"👤 Titular: {person.Name}");
        reply.AppendLine($"📦 Caixinha: {center.Name}");
        reply.AppendLine($"💸 Valor: R$ {amount:N2}");
        reply.AppendLine($"📝 Descrição: {description}");
        reply.AppendLine($"📅 Data: {DateTime.Now:dd/MM/yyyy HH:mm}");
        reply.AppendLine();
        reply.AppendLine($"💼 Novo saldo da caixinha: R$ {newBalance:N2}");

        await _telegramSender.SendMessageAsync(
            chatId,
            reply.ToString(),
            cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.HintAfterCostCenterExpense(),
            cancellationToken);
    }
}
