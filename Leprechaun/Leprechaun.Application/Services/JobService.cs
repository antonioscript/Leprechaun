using Leprechaun.Application.Telegram;
using Leprechaun.Application.Telegram.Flows;
using Leprechaun.Application.Telegram.Flows.Patrimony;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Leprechaun.Application.Services;

public class JobService : IJobService
{
    private readonly IEnumerable<IChatFlow> _flows;
    private readonly long _defaultChatId; // chat que vai receber as mensagens do job

    public JobService(
        IEnumerable<IChatFlow> flows,
        IConfiguration configuration)
    {
        _flows = flows;

        // Configura no appsettings:
        // "Jobs": { "DefaultChatId": "123456789" }
        var chatIdString = configuration["Jobs:DefaultChatId"];

        if (string.IsNullOrWhiteSpace(chatIdString) || !long.TryParse(chatIdString, out _defaultChatId))
            throw new InvalidOperationException("Jobs:DefaultChatId inválido ou não configurado.");
    }

    public async Task RunJob(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        // Usa o mesmo parser dos comandos normais
        var command = TelegramCommandParser.Parse(message);

        switch (command)
        {
            case TelegramCommand.RelatorioPatrimonioPdfEmail:
                await RunMonthlyPatrimonyEmailAsync(command, cancellationToken);
                break;

            // aqui no futuro:
            // case TelegramCommand.AlgumOutroJob:
            //     await RunAlgumOutroJobAsync(...);
            //     break;

            default:
                // por enquanto, ignora comandos desconhecidos
                break;
        }
    }

    private async Task RunMonthlyPatrimonyEmailAsync(TelegramCommand command, CancellationToken cancellationToken)
    {
        // pega o flow específico de relatório PDF+email
        var flow = _flows
            .OfType<PatrimonyPdfEmailReportFlowService>()
            .FirstOrDefault();

        if (flow is null)
            throw new InvalidOperationException("PatrimonyPdfEmailReportFlowService não foi registrado na DI.");

        // ChatState fake, só pra cumprir a assinatura.
        // Esse flow é stateless, então isso é suficiente.
        var state = new ChatState
        {
            ChatId = _defaultChatId,
            State = FlowStates.None,
            UpdatedAt = DateTime.UtcNow
        };

        // Aqui a mágica: chamamos o flow como se o usuário tivesse
        // digitado /relatorio_patrimonio_pdf_email no Telegram.
        await flow.TryHandleAsync(
            chatId: _defaultChatId,
            userText: "/relatorio_email",
            state: state,
            command: command,
            cancellationToken: cancellationToken);
    }
}
