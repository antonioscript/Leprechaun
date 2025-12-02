using System.Globalization;
using System.Linq;
using Leprechaun.Application.Models;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramSender _telegramSender;
    private readonly IPersonService _personService;
    private readonly IChatStateService _chatStateService;
    private readonly IInstitutionService _institutionService;
    private readonly IFinanceTransactionService _transactionService;

    public TelegramController(
        ITelegramSender telegramSender,
        IPersonService personService,
        IChatStateService chatStateService,
        IInstitutionService institutionService,
        IFinanceTransactionService transactionService)
    {
        _telegramSender = telegramSender;
        _personService = personService;
        _chatStateService = chatStateService;
        _institutionService = institutionService;
        _transactionService = transactionService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update, CancellationToken cancellationToken)
    {
        if (update.Message is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return Ok();

        var chatId = update.Message.Chat.Id;
        var userText = update.Message.Text.Trim();

        // Carrega estado do chat a partir do banco
        var state = await _chatStateService.GetAsync(chatId, cancellationToken)
                    ?? new Leprechaun.Domain.Entities.ChatState { ChatId = chatId };

        // --------------------------------------------------------------------
        // FLUXO: /cadastrar_salario
        // --------------------------------------------------------------------

        // PASSO 1 – Usuário começa o fluxo
        if (userText.StartsWith("/cadastrar_salario", StringComparison.OrdinalIgnoreCase))
        {
            // Busca todas as instituições (se quiser, pode filtrar só ativas aqui)
            var institutions = (await _institutionService.GetAllAsync(cancellationToken))
                .Where(i => i.IsActive)
                .ToList();

            if (!institutions.Any())
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Não há instituições cadastradas.",
                    cancellationToken);
                return Ok();
            }

            // Monta a lista numerada
            var reply = "🏦 *Escolha a instituição do salário:*\n\n";
            for (int i = 0; i < institutions.Count; i++)
                reply += $"{i + 1}. {institutions[i].Name}\n";

            // Atualiza estado para aguardar escolha da instituição
            state.State = "AwaitingInstitution";
            state.TempInstitutionId = null;
            state.TempAmount = null;
            await _chatStateService.SaveAsync(state, cancellationToken);

            await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
            return Ok();
        }

        // PASSO 2 – Usuário está escolhendo a instituição
        if (state.State == "AwaitingInstitution")
        {
            if (!int.TryParse(userText, out var index))
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Envie um número válido para escolher a instituição.",
                    cancellationToken);
                return Ok();
            }

            var institutions = (await _institutionService.GetAllAsync(cancellationToken))
                .Where(i => i.IsActive)
                .ToList();

            if (index < 1 || index > institutions.Count)
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Número inválido. Tente novamente.",
                    cancellationToken);
                return Ok();
            }

            var chosen = institutions[index - 1];

            // Salva a instituição escolhida no estado temporário
            state.TempInstitutionId = chosen.Id;
            state.State = "AwaitingAmount";
            await _chatStateService.SaveAsync(state, cancellationToken);

            await _telegramSender.SendMessageAsync(
                chatId,
                $"Informe o valor recebido do salário na instituição *{chosen.Name}*.\nEx: 2560,34",
                cancellationToken);

            return Ok();
        }

        // PASSO 3 – Usuário está enviando o valor
        if (state.State == "AwaitingAmount")
        {
            // Tenta converter o valor digitado
            var normalized = userText.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
            normalized = normalized.Replace(".", "").Replace(",", "."); // 2.560,34 -> 2560.34 (PT-BR clássico)

            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var amount))
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Valor inválido. Tente novamente. Ex: 2560,34",
                    cancellationToken);
                return Ok();
            }

            if (state.TempInstitutionId is null)
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Erro interno: instituição temporária não encontrada.",
                    cancellationToken);
                await _chatStateService.ClearAsync(chatId, cancellationToken);
                return Ok();
            }

            var institution = await _institutionService.GetByIdAsync(state.TempInstitutionId.Value, cancellationToken);
            if (institution is null)
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Erro interno: instituição não encontrada.",
                    cancellationToken);
                await _chatStateService.ClearAsync(chatId, cancellationToken);
                return Ok();
            }

            // Registra a transação de Income (salário) indo para o "salário acumulado" (liquidez)
            await _transactionService.RegisterIncomeAsync(
                personId: institution.PersonId,
                institutionId: institution.Id,
                amount: amount,
                date: DateTime.UtcNow,
                targetCostCenterId: null,      // null => cai no "salário acumulado"
                categoryId: null,
                description: "Salário cadastrado via bot",
                cancellationToken: cancellationToken
            );

            // Limpa o estado do fluxo
            await _chatStateService.ClearAsync(chatId, cancellationToken);

            // Envia comprovante
            var reply =
                $"📄 *Comprovante de Recebimento*\n\n" +
                $"🏦 Instituição: *{institution.Name}*\n" +
                $"💰 Valor: *R$ {amount:N2}*\n" +
                $"📅 Data: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                $"✔ Recebimento registrado com sucesso!";

            await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
            return Ok();
        }

        // --------------------------------------------------------------------
        // OUTROS COMANDOS SIMPLES
        // --------------------------------------------------------------------

        if (userText.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            var reply =
                "🍀 Olá! Eu sou o Leprechaun Bot.\n\n" +
                "Comandos disponíveis:\n" +
                "/help - Lista os comandos\n" +
                "/ping - Teste de conexão\n" +
                "/person - Lista os titulares\n" +
                "/cadastrar_salario - Registrar recebimento de salário\n";

            await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
            return Ok();
        }

        if (userText.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            var reply =
                "📚 *Comandos disponíveis:*\n\n" +
                "/start - Mensagem de boas-vindas\n" +
                "/help - Lista os comandos\n" +
                "/ping - Testa se o bot está online\n" +
                "/person - Lista os titulares da conta\n" +
                "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n";

            await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
            return Ok();
        }

        if (userText.StartsWith("/ping", StringComparison.OrdinalIgnoreCase))
        {
            await _telegramSender.SendMessageAsync(chatId, "Pong! 🏓", cancellationToken);
            return Ok();
        }

        if (userText.StartsWith("/person", StringComparison.OrdinalIgnoreCase))
        {
            var persons = await _personService.GetAllAsync(cancellationToken);

            if (!persons.Any())
            {
                await _telegramSender.SendMessageAsync(chatId,
                    "Nenhum titular encontrado no banco.",
                    cancellationToken);
                return Ok();
            }

            var reply = "👥 *Titulares:*\n\n" +
                        string.Join("\n", persons.Select(p => $"• {p.Name}"));

            await _telegramSender.SendMessageAsync(chatId, reply, cancellationToken);
            return Ok();
        }

        // Fallback: mensagem padrão
        await _telegramSender.SendMessageAsync(
            chatId,
            "Não entendi 🤔\nUse /help para ver os comandos disponíveis.",
            cancellationToken);

        return Ok();
    }
}