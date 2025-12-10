using System.Net;
using System.Net.Mail;
using Leprechaun.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Leprechaun.Application.Services;

public class EmailSender : IEmailSender
{
    private readonly List<string> _recipients = new()
    {
        "antoniojunior159@gmail.com",
        "catarina.braga.design@gmail.com"
    };

    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly bool _useSsl;

    public EmailSender(IConfiguration config)
    {
        _smtpHost = config["Email:SmtpHost"] ?? throw new Exception("Email:SmtpHost missing");
        _smtpPort = int.Parse(config["Email:SmtpPort"] ?? "587");
        _smtpUser = config["Email:User"] ?? throw new Exception("Email:User missing");
        _smtpPass = config["Email:Pass"] ?? throw new Exception("Email:Pass missing");
        _useSsl = bool.Parse(config["Email:UseSsl"] ?? "true");
    }

    public async Task SendPatrimonyReportAsync(
        long chatId,
        DateTime start,
        DateTime end,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = _useSsl,
            Credentials = new NetworkCredential(_smtpUser, _smtpPass)
        };

        foreach (var recipient in _recipients)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_smtpUser, "Leprechaun Finance Bot"),
                Subject = $"Relatório de Despesas - {start:dd/MM/yyyy} a {end:dd/MM/yyyy}",
                Body = "Segue em anexo o relatório mensal de despesas!",
            };

            message.To.Add(recipient);
            message.Attachments.Add(
                new Attachment(new MemoryStream(pdfBytes), "relatorio-despesas.pdf"));

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}