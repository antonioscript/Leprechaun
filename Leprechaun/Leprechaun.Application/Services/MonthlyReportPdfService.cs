using System.Reflection;
using System.Linq;
using Leprechaun.Domain.Interfaces;
using Leprechaun.Domain.Response;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace Leprechaun.Application.Services;

public class MonthlyReportPdfService : IMonthlyReportPdfService
{
    private static byte[] LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Leprechaun.Application.Assets.leprechaun-logo.png";

        using var stream = assembly.GetManifestResourceStream(resourceName)
                      ?? throw new Exception("Logo leprechaun-logo.png não encontrada como EmbeddedResource.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public byte[] GeneratePatrimonyReportPdf(string title, PatrimonyReportDto data)
    {
        var logoBytes = LoadLogo();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                page.DefaultTextStyle(x =>
                    x.FontSize(12).FontFamily(Fonts.Verdana));

                // ========== HEADER ==========
                page.Header().Row(row =>
                {
                    // logo maior
                    row.ConstantColumn(90)
                        .Height(60)
                        .Image(logoBytes, ImageScaling.FitArea);

                    row.RelativeColumn().Column(col =>
                    {
                        col.Item().Text("Leprechaun Finance")
                            .FontSize(26)
                            .Bold()
                            .FontColor("#1B5E20");

                        col.Item().Text(title)
                            .FontSize(13)
                            .FontColor("#555555");
                    });
                });

                // ========== CONTENT ==========
                page.Content().PaddingTop(25).Column(col =>
                {
                    col.Spacing(18);

                    // --- Título principal ---
                    col.Item().Text("Relatório de Patrimônio")
                        .FontSize(20)
                        .Bold()
                        .FontColor("#1B5E20");

                    // ============================
                    //         VISÃO GERAL
                    // ============================
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Visão Geral")
                            .FontSize(16)
                            .Bold()
                            .FontColor("#33691E");

                        sec.Item().Text($"Entradas: R$ {data.GeneralEntries:N2}");
                        sec.Item().Text($"Saídas externas: R$ {data.GeneralOutflows:N2}");
                        sec.Item().Text($"Saldo (Entradas - Saídas externas): R$ {data.GeneralBalance:N2}");
                    });

                    // ============================
                    //      SALÁRIO ACUMULADO
                    // ============================
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Salário Acumulado")
                            .FontSize(16)
                            .Bold()
                            .FontColor("#33691E");

                        sec.Item().Text($"Entradas: R$ {data.SalaryEntries:N2}");
                        sec.Item().Text($"Saídas: R$ {data.SalaryOutflows:N2}");

                        if (data.SalaryExpenses.Any())
                        {
                            sec.Item().PaddingTop(8).Text("Despesas (salário acumulado)")
                                .FontSize(13)
                                .Bold();

                            // linhas estilo extrato
                            sec.Item().Column(list =>
                            {
                                int index = 0;
                                foreach (var exp in data.SalaryExpenses.OrderBy(e => e.Date))
                                {
                                    var bg = index % 2 == 0 ? "#F0F0F0" : "#DADADA";
                                    index++;

                                    list.Item()
                                        .Background(bg)
                                        .Padding(8)
                                        .Row(row =>
                                        {
                                            row.RelativeColumn().Text(
                                                $"{exp.Date:dd/MM/yyyy}  |  {exp.Description}");

                                            row.ConstantColumn(110)
                                                .AlignRight()
                                                .Text($"R$ {exp.Amount:N2}");
                                        });
                                }
                            });
                        }
                    });

                    // ============================
                    //          CAIXINHAS
                    // ============================
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Caixinhas")
                            .FontSize(16)
                            .Bold()
                            .FontColor("#33691E");

                        foreach (var cc in data.CostCenters.OrderBy(c => c.Name))
                        {
                            sec.Item().PaddingTop(12).Column(box =>
                            {
                                // Título da caixinha
                                box.Item().Text(cc.Name)
                                    .FontSize(14)
                                    .Bold();

                                if (cc.Expenses.Any())
                                {
                                    // 🟩 Espaço entre nome → tabela
                                    box.Item().PaddingTop(6).Column(list =>
                                    {
                                        int index = 0;
                                        foreach (var exp in cc.Expenses.OrderBy(e => e.Date))
                                        {
                                            var bg = index % 2 == 0 ? "#F0F0F0" : "#DADADA";
                                            index++;

                                            list.Item()
                                                .Background(bg)
                                                .Padding(8)
                                                .Row(row =>
                                                {
                                                    row.RelativeColumn()
                                                        .Text($"{exp.Date:dd/MM/yyyy}  |  {exp.Description}");

                                                    row.ConstantColumn(110)
                                                        .AlignRight()
                                                        .Text($"R$ {exp.Amount:N2}");
                                                });
                                        }
                                    });
                                }

                                // 🟩 Espaço entre tabela → total
                                box.Item().PaddingTop(6)
                                    .Text($"Total de despesas: R$ {cc.TotalExpenses:N2}")
                                    .FontSize(12)
                                    .Bold();
                            });
                        }
                        var totalCenters = data.CostCenters.Sum(c => c.TotalExpenses);
                        sec.Item().PaddingTop(12)
                            .Text($"Total de despesas em caixinhas: R$ {totalCenters:N2}")
                            .FontSize(13)
                            .Bold();
                    });
                });

                // ========== FOOTER ==========
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Gerado por Leprechaun Finance · ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                });
            });
        });

        return document.GeneratePdf();
    }
}