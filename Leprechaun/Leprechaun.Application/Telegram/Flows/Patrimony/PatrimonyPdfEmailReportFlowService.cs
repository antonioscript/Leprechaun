using Leprechaun.Application.Telegram;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Interfaces;

namespace Leprechaun.Application.Telegram.Flows.Patrimony;

public class PatrimonyPdfEmailReportFlowService : IChatFlow
{
    private readonly IChatStateService _chatStateService;
    private readonly IPatrimonyReportService _patrimonyReportService;
    private readonly IMonthlyReportPdfService _pdfService;
    private readonly ITelegramSender _telegramSender;
    private readonly IEmailSender _emailSender;

    public PatrimonyPdfEmailReportFlowService(
        IChatStateService chatStateService,
        IPatrimonyReportService patrimonyReportService,
        IMonthlyReportPdfService pdfService,
        ITelegramSender telegramSender,
        IEmailSender emailSender)
    {
        _chatStateService = chatStateService;
        _patrimonyReportService = patrimonyReportService;
        _pdfService = pdfService;
        _telegramSender = telegramSender;
        _emailSender = emailSender;
    }

    public async Task<bool> TryHandleAsync(
        long chatId,
        string userText,
        ChatState state,
        TelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (command != TelegramCommand.RelatorioPatrimonioPdfEmail)
            return false;

        await _chatStateService.ClearAsync(chatId, cancellationToken);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = now;
        
        /*
        await _telegramSender.SendMessageAsync(
            chatId,
            $"Gerando relatório de patrimônio em PDF e enviando por e-mail...\n" +
            $"Período: {startOfMonth:dd/MM/yyyy} - {end:dd/MM/yyyy}",
            cancellationToken);
        */

        // 1) Dados
        var data = await _patrimonyReportService.BuildPatrimonyReportDataAsync(
            startOfMonth,
            end,
            cancellationToken);

        // 2) PDF
        var title = $"Relatório de Patrimônio ({startOfMonth:dd/MM/yyyy} - {end:dd/MM/yyyy})";
        var pdfBytes = _pdfService.GeneratePatrimonyReportPdf(title, data);

        // 3) Envio por e-mail
        try
        {
            await _emailSender.SendPatrimonyReportAsync(
                chatId,
                startOfMonth,
                end,
                pdfBytes,
                cancellationToken);
            
            /*
            await _telegramSender.SendMessageAsync(
                chatId,
                "Relatório enviado com sucesso para os e-mails padrão.",
                cancellationToken);
                
            */
        }
        catch (Exception)
        {
            await _telegramSender.SendMessageAsync(
                chatId,
                "Ocorreu um erro ao enviar o relatório por e-mail. Verifique os logs.",
                cancellationToken);
        }

        return true;
    }
}