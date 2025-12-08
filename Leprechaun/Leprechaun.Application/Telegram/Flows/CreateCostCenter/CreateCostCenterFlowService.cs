using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Enums;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.CreateCostCenter;

public class CreateCostCenterFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPersonService _personService;
    private readonly ICostCenterService _costCenterService;
    private readonly ITelegramSender _telegramSender;

    public CreateCostCenterFlowService(
        IChatStateService chatStateService,
        IPersonService personService,
        ICostCenterService costCenterService,
        ITelegramSender telegramSender)
    {
        _chatStateService = chatStateService;
        _personService = personService;
        _costCenterService = costCenterService;
        _telegramSender = telegramSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        // 1) Se já estamos em algum passo do fluxo, continua
        if (state.State == FlowStates.CostCenterAwaitingName ||
            state.State == FlowStates.CostCenterAwaitingOwner ||
            state.State == FlowStates.CostCenterAwaitingType)
        {
            await HandleOngoingFlowAsync(chatId, userText, state, cancellationToken);
            return true;
        }

        // 2) Se é o comando /criar_caixinha, inicia o fluxo
        if (command == TelegramCommand.CriarCaixinha)
        {
            await StartFlowAsync(chatId, state, cancellationToken);
            return true;
        }

        // 3) Não é com esse fluxo
        return false;
    }

    // ---------- Início do fluxo ----------

    private async Task StartFlowAsync(
        long chatId,
        ChatState state,
        CancellationToken cancellationToken)
    {
        state.State = FlowStates.CostCenterAwaitingName;
        state.TempCostCenterName = null;
        state.TempPersonId = null;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            "📦 Informe o *nome da nova caixinha*:",
            cancellationToken);
    }

    // ---------- Continuação do fluxo ----------

    private async Task HandleOngoingFlowAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.State == FlowStates.CostCenterAwaitingName)
        {
            await HandleNameAsync(chatId, userText, state, cancellationToken);
        }
        else if (state.State == FlowStates.CostCenterAwaitingOwner)
        {
            await HandleOwnerAsync(chatId, userText, state, cancellationToken);
        }
        else if (state.State == FlowStates.CostCenterAwaitingType)
        {
            await HandleTypeAsync(chatId, userText, state, cancellationToken);
        }
    }

    private async Task HandleNameAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ O nome da caixinha não pode ser vazio. Tente novamente.",
                cancellationToken);
            return;
        }

        state.TempCostCenterName = userText.Trim();
        state.State = FlowStates.CostCenterAwaitingOwner;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        var persons = (await _personService.GetAllAsync(cancellationToken)).ToList();

        if (!persons.Any())
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Não há titulares cadastrados para associar à caixinha.",
                cancellationToken);

            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var text = $"👤 Selecione o *titular* da caixinha \"{state.TempCostCenterName}\":";

        // callback_data = personId
        var buttons = persons
            .Select(p => (Label: p.Name, Data: p.Id.ToString()))
            .ToList();

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            text,
            buttons,
            cancellationToken);
    }

    private async Task HandleOwnerAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state.TempCostCenterName))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno: nome da caixinha perdido. Comece novamente com /criar_caixinha.",
                cancellationToken);

            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        if (!int.TryParse(userText, out var personId))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Titular inválido. Tente clicar novamente no botão.",
                cancellationToken);
            return;
        }

        state.TempPersonId = personId;
        state.State = FlowStates.CostCenterAwaitingType;
        state.UpdatedAt = DateTime.UtcNow;

        await _chatStateService.SaveAsync(state, cancellationToken);

        // Pergunta o tipo da caixinha
        var buttons = new List<(string Label, string Data)>
        {
            ("Default (normal)", "type_default"),
            ("Proibida despesa direta", "type_blocked"),
            ("Infra mensal", "type_infra")
        };

        await _telegramSender.SendMessageWithInlineKeyboardAsync(
            chatId,
            "🏷️ *Escolha o tipo da caixinha:*",
            buttons,
            cancellationToken);
    }

    private async Task HandleTypeAsync(
        long chatId,
        string userText,
        ChatState state,
        CancellationToken cancellationToken)
    {
        if (state.TempPersonId is null || string.IsNullOrWhiteSpace(state.TempCostCenterName))
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "⚠️ Erro interno. Recomece com /criar_caixinha.",
                cancellationToken);
            await _chatStateService.ClearAsync(chatId, cancellationToken);
            return;
        }

        var type = userText switch
        {
            "type_default" => CostCenterType.Default,
            "type_blocked" => CostCenterType.ProibidaDespesaDireta,
            "type_infra" => CostCenterType.InfraMensal,
            _ => CostCenterType.Default
        };

        // Se for InfraMensal, validar se já existe alguma
        if (type == CostCenterType.InfraMensal)
        {
            var all = await _costCenterService.GetAllAsync(cancellationToken);
            if (all.Any(c => c.Type == CostCenterType.InfraMensal))
            {
                await _telegramSender.SendMessageAsync(
                    chatId,
                    "⚠️ Já existe uma caixinha do tipo Infra Mensal. Atualemnte Só é permitido existir uma. ",
                    cancellationToken);

                await _chatStateService.ClearAsync(chatId, cancellationToken);
                return;
            }
        }

        var costCenter = await _costCenterService.CreateAsync(
            state.TempCostCenterName,
            state.TempPersonId.Value,
            type,
            cancellationToken);

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            $"✅ Caixinha *{costCenter.Name}* criada com sucesso!\n" +
            $"🏷️ Tipo: *{type}*",
            cancellationToken);

        await _telegramSender.SendMessageAsync(
            chatId,
            BotTexts.HintAfterCreateCostCenter(),
            cancellationToken);
    }
}
