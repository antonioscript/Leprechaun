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
    private readonly IExpenseService _expenseService;
    private readonly ITelegramSender _telegramSender;

    public RegisterCostCenterExpenseFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        ICostCenterService costCenterService,
        IFinanceTransactionService transactionService,
        IExpenseService expenseService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _costCenterService = costCenterService;
        _transactionService = transactionService;
        _expenseService = expenseService;
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
            state.State == FlowStates.CostCenterExpenseAwaitingDescription ||
            state.State == FlowStates.CostCenterExpenseAwaitingInfraTemplate ||
            state.State == FlowStates.CostCenterExpenseAwaitingInfraConfirmOrAdjust ||
            state.State == FlowStates.CostCenterExpenseAwaitingInfraCustomAmount)
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
        state.TempInstitutionId = null; // vamos usar para guardar o template (Expense.Id) em Infra
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

            case FlowStates.CostCenterExpenseAwaitingInfraTemplate:
                await HandleInfraTemplateAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterExpenseAwaitingInfraConfirmOrAdjust:
                await HandleInfraConfirmOrAdjustAsync(chatId, userText, state, cancellationToken);
                break;

            case FlowStates.CostCenterExpenseAwaitingInfraCustomAmount:
                await HandleInfraCustomAmountAsync(chatId, userText, state, cancellationToken);
                break;
        }
    }

    // -------- 1) ESCOLHER TITULAR --------

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
                        c.Type != CostCenterType.ProibidaDespesaDireta) // não mostra proibida
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

    // -------- 2) ESCOLHER CAIXINHA (normal x infra) --------

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

        if (center.Type == CostCenterType.InfraMensal)
        {
            // 🔹 Fluxo ESPECIAL INFRA: escolher tipo de despesa (template)
            state.State = FlowStates.CostCenterExpenseAwaitingInfraTemplate;
            state.UpdatedAt = DateTime.UtcNow;
            await _chatStateService.SaveAsync(state, cancellationToken);

            var templates = await _expenseService.GetByCostCenterAsync(centerId, cancellationToken);
            if (!templates.Any())
            {
                await _telegramSender.SendMessageAsync(
                    chatId,
                    "⚠️ Não há tipos de despesa cadastrados para essa caixinha de infra.",
                    cancellationToken);
                await _chatStateService.ClearAsync(chatId, cancellationToken);
                return;
            }

            var buttons = templates
                .Select(t => (Label: t.Name, Data: $"infra_tpl_{t.Id}"))
                .ToList();

            await _telegramSender.SendMessageWithInlineKeyboardAsync(
                chatId,
                "🏷️ *Escolha o tipo de despesa (Infra):*",
                buttons,
                cancellationToken);

            return;
        }

        // 🔹 Fluxo NORMAL (caixinha padrão)
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

    // -------- 3A) VALOR (fluxo normal) --------

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
                $"⚠️ Saldo insuficiente.\nSaldo atual: R$ {balance:N2}",
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

    // -------- 4A) DESCRIÇÃO (fluxo normal) --------

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

    // ==========================
    //      FLUXO INFRA MENSAL
    // ==========================

    // 3B.1) Escolher template de despesa (Internet, Energia, etc.)

    private async Task HandleInfraTemplateAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempSourceCostCenterId is null || state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno. Recomece com /registrar_despesa_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        if (!userText.StartsWith("infra_tpl_", StringComparison.OrdinalIgnoreCase))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Escolha um tipo de despesa válido.",
                cancellationToken);
            return;
        }

        var idPart = userText["infra_tpl_".Length..];
        if (!int.TryParse(idPart, out var templateId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Tipo de despesa inválido. Tente novamente.",
                cancellationToken);
            return;
        }

        var template = await _expenseService.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Tipo de despesa não encontrado.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var expected = template.DefaultAmount ?? 0m;

        // Guardar o templateId para o caso de "Ajustar valor"
        state.TempInstitutionId = template.Id;
        state.State = FlowStates.CostCenterExpenseAwaitingInfraConfirmOrAdjust;
        state.UpdatedAt = DateTime.UtcNow;
        await _chatStateService.SaveAsync(state, cancellationToken);

        var msg = new StringBuilder();
        msg.AppendLine($"📡 Tipo de despesa: {template.Name}");
        msg.AppendLine($"💰 Valor médio esperado: R$ {expected:N2}");
        msg.AppendLine();
        msg.AppendLine("O que você deseja fazer?");

        var buttons = new List<(string Label, string Data)>
        {
            ("✅ Confirmar esse valor", $"infra_confirm_{template.Id}"),
            ("✏️ Ajustar valor", $"infra_adjust_{template.Id}")
        };

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            msg.ToString(),
            buttons,
            cancellationToken);
    }

    // 3B.2) Confirmar ou ajustar valor

    private async Task HandleInfraConfirmOrAdjustAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempSourceCostCenterId is null || state.TempPersonId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno. Recomece com /registrar_despesa_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var isConfirm = userText.StartsWith("infra_confirm_", StringComparison.OrdinalIgnoreCase);
        var isAdjust = userText.StartsWith("infra_adjust_", StringComparison.OrdinalIgnoreCase);

        if (!isConfirm && !isAdjust)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Opção inválida. Tente novamente.",
                cancellationToken);
            return;
        }

        var prefix = isConfirm ? "infra_confirm_" : "infra_adjust_";
        var idPart = userText[prefix.Length..];

        if (!int.TryParse(idPart, out var templateId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Tipo de despesa inválido.",
                cancellationToken);
            return;
        }

        var template = await _expenseService.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Tipo de despesa não encontrado.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var expected = template.DefaultAmount ?? 0m;

        if (isConfirm)
        {
            // Usa diretamente o valor esperado
            await RegisterInfraExpenseAndSendReceiptAsync(
                chatId,
                state,
                template,
                expected,
                expected,
                cancellationToken);
        }
        else
        {
            // Ajustar valor -> próximo passo: pedir o valor
            state.TempInstitutionId = template.Id; // guardar template
            state.State = FlowStates.CostCenterExpenseAwaitingInfraCustomAmount;
            state.UpdatedAt = DateTime.UtcNow;
            await _chatStateService.SaveAsync(state, cancellationToken);

            var msg = new StringBuilder();
            msg.AppendLine($"📡 Tipo de despesa: {template.Name}");
            msg.AppendLine($"💰 Valor médio esperado: R$ {expected:N2}");
            msg.AppendLine();
            msg.AppendLine("Digite o valor que você pagou (ex: 250,00):");

            await _telegramSender.SendMessageAsync(
                chatId,
                msg.ToString(),
                cancellationToken);
        }
    }

    // 3B.3) Usuário informar valor customizado

    private async Task HandleInfraCustomAmountAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempSourceCostCenterId is null ||
            state.TempPersonId is null ||
            state.TempInstitutionId is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno. Recomece com /registrar_despesa_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var templateId = (int)state.TempInstitutionId.Value;
        var template = await _expenseService.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Tipo de despesa não encontrado.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var normalized = userText.Replace("R$", "", StringComparison.OrdinalIgnoreCase)
                                 .Replace(".", "")
                                 .Replace(",", ".")
                                 .Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var paid) ||
            paid <= 0)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Valor inválido. Digite novamente (ex: 250,00).",
                cancellationToken);
            return;
        }

        var expected = template.DefaultAmount ?? 0m;

        await RegisterInfraExpenseAndSendReceiptAsync(
            chatId,
            state,
            template,
            expected,
            paid,
            cancellationToken);
    }

    // -------- REGISTRO + COMPROVANTE INFRA --------

    private async Task RegisterInfraExpenseAndSendReceiptAsync(
        long chatId,
        ChatState state,
        Expense template,
        decimal expectedAmount,
        decimal paidAmount,
        CancellationToken cancellationToken)
    {
        var personId = state.TempPersonId!.Value;
        var centerId = state.TempSourceCostCenterId!.Value;

        // Verifica saldo
        var balance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);
        if (paidAmount > balance)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                $"⚠️ Saldo insuficiente na caixinha.\nSaldo atual: *R$ {balance:N2}*",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        await _transactionService.RegisterExpenseFromCostCenterAsync(
            personId,
            centerId,
            paidAmount,
            DateTime.UtcNow,
            categoryId: template.CategoryId,
            description: template.Name,
            cancellationToken);

        var newBalance = await _transactionService.GetCostCenterBalanceAsync(centerId, cancellationToken);

        var persons = await _personService.GetAllAsync(cancellationToken);
        var person = persons.First(p => p.Id == personId);

        var centers = await _costCenterService.GetAllAsync(cancellationToken);
        var center = centers.First(c => c.Id == centerId);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        // Monta o texto de comparação
        var comparison = BuildInfraComparisonText(expectedAmount, paidAmount, out var emoji);

        var reply = new StringBuilder();
        reply.AppendLine("✅ Despesa de infra registrada com sucesso!");
        reply.AppendLine();
        reply.AppendLine($"👤 Titular: {person.Name}");
        reply.AppendLine($"📦 Caixinha (Infra): {center.Name}");
        reply.AppendLine($"📡 Tipo de despesa: {template.Name}");
        reply.AppendLine($"💰 Valor esperado: R$ {expectedAmount:N2}");
        reply.AppendLine($"💸 Valor pago: R$ {paidAmount:N2}");
        reply.AppendLine($"📅 Data: {DateTime.Now:dd/MM/yyyy HH:mm}");
        reply.AppendLine();
        reply.AppendLine($"{emoji} {comparison}");
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

    private static string BuildInfraComparisonText(decimal expected, decimal paid, out string emoji)
    {
        if (expected <= 0)
        {
            emoji = "ℹ️";
            return "Valor esperado não configurado. Comparação não disponível.";
        }

        if (paid == expected)
        {
            emoji = "✅";
            return "Despesa dentro do esperado para este tipo de gasto.";
        }

        if (paid < expected)
        {
            emoji = "🟢";
            return "Ótimo! A despesa ficou abaixo do valor esperado para este tipo de gasto!";
        }

        emoji = "😕";
        return "Atenção: a despesa ficou acima do valor esperado para este tipo de gasto.";
    }
}
