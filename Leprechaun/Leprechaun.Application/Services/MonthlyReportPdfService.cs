using System.Reflection.Metadata;
using Leprechaun.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace Leprechaun.Application.Services;

public class MonthlyReportPdfService : IMonthlyReportPdfService
{
    public byte[] GeneratePatrimonyReportPdf(string title, string reportText)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Text(title)
                    .SemiBold().FontSize(20).AlignCenter();

                page.Content().PaddingTop(10).Element(c =>
                {
                    c.Text(reportText)
                        .FontSize(10)
                        .FontFamily(Fonts.Consolas); // deixa “cara” de relatório de console
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Gerado por Leprechaun Bot ");
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                });
            });
        });

        return document.GeneratePdf();
    }
}