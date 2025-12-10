using System.Text;
using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("email")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailSender _emailSender;

    public EmailTestController(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    [HttpGet("test-patrimony")]
    public async Task<IActionResult> TestPatrimonyEmail(CancellationToken cancellationToken)
    {
        var start = new DateTime(2025, 12, 1);
        var end = new DateTime(2025, 12, 21);

        // PDF fake só p/ testar anexo (depois você troca pelo PdfService real)
        var fakeText = "Relatório de Despesas (teste)\nGerado pelo Leprechaun Bot.";
        var pdfBytes = Encoding.UTF8.GetBytes(fakeText); // funciona como anexo mesmo assim

        await _emailSender.SendPatrimonyReportAsync(
            chatId: 0,
            start: start,
            end: end,
            pdfBytes: pdfBytes,
            cancellationToken: cancellationToken);

        return Ok(new
        {
            Message = "E-mail de teste enviado com anexo (fake).",
            Period = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}"
        });
    }
}