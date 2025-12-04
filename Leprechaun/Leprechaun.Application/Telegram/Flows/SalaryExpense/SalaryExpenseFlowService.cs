using System.Globalization;
using System.Text;
using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.SalaryExpense;

public class SalaryExpenseFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly IFinanceTransactionService _transactionService;
    private readonly ITelegramSender _telegramSender;

    public SalaryExpenseFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        IFinanceTransactionService transactionService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
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
        // Já está no fluxo?
        if (state.State == FlowStates.SalaryExpenseAwaitingPerson ||
            state.State == FlowStates.SalaryExpenseAwaitingAmount ||
            state.State == FlowStates.SalaryExpenseAwaitingDescription)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // Comando para iniciar o fluxo
        if (command == TelegramCommand.RegistrarDespesaSalAcml)
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
        state.State = FlowStates.SalaryExpenseAwaitingPerson;
        state.TempPersonId = null;
        state.TempAmount = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var persons = (await _personService.GetAllAsync(cancellationToken)).ToList();
        if (!persons.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não há titulares cadastrados para registrar a despesa.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = "👤 Selecione o *titular* da despesa do salário acumulado:";

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
            case FlowStates.SalaryExpenseAwaitingPerson:
                await HandlePersonAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.SalaryExpenseAwaitingAmount:
                await HandleAmountAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.SalaryExpenseAwaitingDescription:
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
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        state.TempPersonId = personId;
        state.State = FlowStates.SalaryExpenseAwaitingAmount;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        // Saldo do titular
        var personBalance = await _transactionService.GetSalaryAccumulatedAsync(personId, cancellationToken);

        // Saldo total (todos titulares)
        var totalBalance = await _transactionService.GetTotalSalaryAccumulatedAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("💸 *Registro de despesa do salário acumulado*");
        sb.AppendLine($"Titular selecionado: *{person.Name}*");
        sb.AppendLine();
        sb.AppendLine($"💼 Saldo atual do titular: *R$ {personBalance:N2}*\n");
        sb.AppendLine($"📊 Saldo acumulado total (todos): *R$ {totalBalance:N2}*");
        sb.AppendLine();
        sb.AppendLine("Por favor, informe o *valor da despesa* (ex: 250,00):");

        await _telegramSender.SendMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken);
    }

    private async Task HandleAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: titular não definido. Recomece com /registrar_despesa_sal_acml.",
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
        var personBalance = await _transactionService.GetSalaryAccumulatedAsync(personId, cancellationToken);

        if (amount > personBalance)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                $"⚠️ Saldo insuficiente.\n\n Saldo atual do titular: *R$ {personBalance:N2}*.\\nnInforme um valor menor.",
                cancellationToken);
            return;
        }

        state.TempAmount = amount;
        state.State = FlowStates.SalaryExpenseAwaitingDescription;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "📝 Informe a *descrição da despesa*:",
            cancellationToken);
    }

    private async Task HandleDescriptionAsync(
    long chatId,
    string userText,
    ChatState state,
    CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null || state.TempAmount is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: dados incompletos. Recomece com /registrar_despesa_sal_acml.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var description = userText?.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ A descrição não pode ser vazia. Informe um nome para a despesa.",
                cancellationToken);
            return;
        }

        var personId = state.TempPersonId.Value;
        var amount = state.TempAmount.Value;

        // Recupera nome do titular
        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault(p => p.Id == personId);

        // Registrar despesa (saída do salário acumulado)
        await _transactionService.RegisterExpenseFromSalaryAsync(
            personId,
            amount,
            DateTime.UtcNow,
            categoryId: null,
            description: description,
            cancellationToken: cancellationToken);

        // Novo saldo do titular
        var newBalance = await _transactionService.GetSalaryAccumulatedAsync(personId, cancellationToken);

        // Novo saldo total dos titulares
        var totalBalance = await _transactionService.GetTotalSalaryAccumulatedAsync(cancellationToken);

        // Limpar estado do fluxo
        await _chatStateService.ClearAsync(chatId, cancellationToken);

        // Montar resposta
        var reply = new StringBuilder();
        reply.AppendLine("✅ *Despesa registrada com sucesso!*");
        reply.AppendLine();
        reply.AppendLine($"👤 *Titular:* {person?.Name}");
        reply.AppendLine($"💸 *Valor:* R$ {amount:N2}");
        reply.AppendLine($"📝 *Descrição:* {description}");
        reply.AppendLine($"📅 *Data:* {DateTime.Now:dd/MM/yyyy HH:mm}");
        reply.AppendLine();
        reply.AppendLine($"💼 *Novo saldo do titular:* R$ {newBalance:N2}");
        reply.AppendLine();
        reply.AppendLine($"🌎 *Novo saldo total acumulado:* R$ {totalBalance:N2}");

        await _telegramSender.SendMessageAsync(
            chatId,
            reply.ToString(),
            cancellationToken);
    }

}
