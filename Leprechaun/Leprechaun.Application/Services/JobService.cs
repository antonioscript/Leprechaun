using Leprechaun.Application.Telegram;
using Leprechaun.Application.Telegram.Flows;
using Leprechaun.Application.Telegram.Flows.Patrimony;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Leprechaun.Application.Services;

public class JobService : IJobService
{
    private readonly IEnumerable<IChatFlow> _flows;
    private readonly long _defaultChatId; // chat que vai receber as mensagens do job
    private readonly ITelegramSender _telegramSender;

    public JobService(
        IEnumerable<IChatFlow> flows,
        IConfiguration configuration,
        ITelegramSender telegramSender)
    {
        _flows = flows;
        _telegramSender = telegramSender;

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

        var command = TelegramCommandParser.Parse(message);

        switch (command)
        {
            case TelegramCommand.RelatorioPatrimonioPdfEmail:
                await RunMonthlyPatrimonyEmailAsync(command, cancellationToken);
                break;

            case TelegramCommand.NotificationPepsiSalaryFriday:
                await RunNotiticationPepsiSalary(command, cancellationToken);
                break;
            
            case TelegramCommand.NotificationSalaryBiweekly:
                await RunNotiticationBiweeklySalaryIfNeeded(command, cancellationToken);
                break;

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

    private async Task RunNotiticationPepsiSalary(TelegramCommand command, CancellationToken cancellationToken)
    {
        var text = """
            💰 Lembrete de salário Pepsi!

            @Catarina_Sophia, hoje é sexta, o dia em que o salário da Pepsi costuma cair. 

            Não esqueça de lançar as entradas no Leprechaun.  
            A princesa Leprechaun agradece, e o Imposto da Vila também (evitar multas mantém nossa magia em dia 🍀✨)

            É só usar o comando /cadastrar_salario e informar o valor.

            Se já lançou, pode ignorar esta mensagem 😉
            """;


        await _telegramSender.SendMessageAsync(_defaultChatId, text, cancellationToken);
    }


    private async Task RunNotiticationBiweeklySalaryIfNeeded(TelegramCommand command, CancellationToken cancellationToken)
    {
        var today = GetTodayInFortaleza().Date;

        if (!ShouldSendBiweeklySalaryReminder(today))
        {
            // Hoje não é dia de lembrete (nem 15 ajustado, nem último dia ajustado)
            return;
        }

        await RunNotiticationbiweeklySalary(command, cancellationToken);
    }

    private async Task RunNotiticationbiweeklySalary(TelegramCommand command, CancellationToken cancellationToken)
    {
        var text = """
            💰 Lembrete de salário!

            @Catarina_Sophia e @Antonio_dc19, hoje é um dia abençoado para três nobres instituições:

            • Banco Safra  
            • Banco Itaú  
            • Genial Investimentos  

            Dia de din-din na conta! ✨🍀

            Não esqueçam de lançar as entradas no Leprechaun. 
            A Vila agradece, a contabilidade sorri e a magia financeira permanece viva. 🧙‍♂️💸

            É só usar o comando /cadastrar_salario e informar o valor.

            Se já lançaram, podem ignorar esta mensagem e aproveitar a prosperidade 😉
            """;

        await _telegramSender.SendMessageAsync(_defaultChatId, text, cancellationToken);
    }


    #region Helpers
    private static DateTime GetTodayInFortaleza()
    {
        TimeZoneInfo? tz = null;

        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch
        {
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch
            {
                // fallback: usa UTC mesmo
                return DateTime.UtcNow.Date;
            }
        }

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    private static bool IsBusinessDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday &&
               date.DayOfWeek != DayOfWeek.Sunday;
        // Se quiser, depois adicionamos feriados aqui.
    }

    private static DateTime AdjustToPreviousBusinessDay(DateTime date)
    {
        var d = date;
        while (!IsBusinessDay(d))
            d = d.AddDays(-1);

        return d;
    }

    /// <summary>
    /// Regra: disparar em 2 datas por mês:
    /// - dia 15 ajustado para o dia útil anterior (se cair fim de semana)
    /// - último dia do mês ajustado para o dia útil anterior
    /// </summary>
    private static bool ShouldSendBiweeklySalaryReminder(DateTime today)
    {
        var year = today.Year;
        var month = today.Month;

        var fifteenth = new DateTime(year, month, 15);
        var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var payDate1 = AdjustToPreviousBusinessDay(fifteenth);
        var payDate2 = AdjustToPreviousBusinessDay(lastDayOfMonth);

        return today.Date == payDate1.Date || today.Date == payDate2.Date;
    }


    #endregion
}
