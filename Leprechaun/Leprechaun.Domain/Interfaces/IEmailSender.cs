// Leprechaun.Domain/Interfaces/IEmailSender.cs
namespace Leprechaun.Domain.Interfaces;

public interface IEmailSender
{
    Task SendPatrimonyReportAsync(
        long chatId,
        DateTime start,
        DateTime end,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);
}