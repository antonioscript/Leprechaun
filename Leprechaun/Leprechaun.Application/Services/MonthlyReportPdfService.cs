using System.Linq;
using System.Reflection;
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

        // totais para usar no relatório
        var totalCaixinhas = data.CostCenters.Sum(c => c.TotalExpenses);
        var totalDespesasSalario = data.SalaryOutflows; // equivalente à soma das despesas do salário acumulado
        var totalDespesasGeral = totalDespesasSalario + totalCaixinhas;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);

                page.DefaultTextStyle(x =>
                    x.FontSize(11).FontFamily(Fonts.Verdana));

                // ========== HEADER ==========
                page.Header().Row(row =>
                {
                    // logo maior
                    row.ConstantColumn(90)
                        .Height(70)
                        .Image(logoBytes, ImageScaling.FitArea);

                    row.RelativeColumn().Column(col =>
                    {
                        col.Item().Text("Leprechaun Finance")
                            .FontSize(22)
                            .Bold()
                            .FontColor("#1B5E20");

                        col.Item().Text(title)
                            .FontSize(12)
                            .FontColor("#555555");
                    });
                });

                // ========== CONTENT ==========
                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(16);

                    // --- Título principal ---
                    col.Item().Text("Relatório de Patrimônio")
                        .FontSize(16)
                        .Bold()
                        .FontColor("#1B5E20");

                    // --- Visão Geral ---
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Visão Geral")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#33691E");

                        sec.Item().Text($"Entradas: R$ {data.GeneralEntries:N2}");
                        sec.Item().Text($"Saídas externas: R$ {data.GeneralOutflows:N2}");
                        sec.Item().Text($"Saldo (Entradas - Saídas externas): R$ {data.GeneralBalance:N2}");
                    });

                    // --- Salário Acumulado ---
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Salário Acumulado")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#33691E");

                        sec.Item().Text($"Entradas: R$ {data.SalaryEntries:N2}");
                        sec.Item().Text($"Saídas: R$ {data.SalaryOutflows:N2}");

                        if (data.SalaryExpenses.Any())
                        {
                            // tabela de despesas (sem o título "Despesas ...")
                            bool odd = false;

                            foreach (var exp in data.SalaryExpenses.OrderBy(e => e.Date))
                            {
                                var localOdd = odd;
                                sec.Item()
                                    .PaddingTop(4)
                                    .Background(localOdd ? "#F5F5F5" : "#EEEEEE")
                                    .Padding(6)
                                    .Row(row =>
                                    {
                                        row.RelativeColumn()
                                            .Text($"{exp.Date:dd/MM/yyyy} | {exp.Description}");

                                        row.ConstantColumn(100)
                                            .AlignRight()
                                            .Text($"R$ {exp.Amount:N2}");
                                    });

                                odd = !odd;
                            }

                            // total de despesas do salário acumulado logo abaixo da tabela
                            sec.Item().PaddingTop(6)
                                .Text($"Total de despesas do salário acumulado: R$ {totalDespesasSalario:N2}")
                                .FontSize(11)
                                .Bold();
                        }
                    });

                    // --- Caixinhas ---
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Caixinhas")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#33691E");

                        foreach (var cc in data.CostCenters.OrderBy(c => c.Name))
                        {
                            // nome da caixinha com um pequeno espaço antes da tabela
                            sec.Item().PaddingTop(12).Text(cc.Name)
                                .FontSize(12)
                                .Bold();

                            if (cc.Expenses.Any())
                            {
                                bool odd = false;

                                foreach (var exp in cc.Expenses.OrderBy(e => e.Date))
                                {
                                    var localOdd = odd;

                                    sec.Item()
                                        .PaddingTop(4)
                                        .Background(localOdd ? "#F5F5F5" : "#EEEEEE")
                                        .Padding(6)
                                        .Row(row =>
                                        {
                                            row.RelativeColumn()
                                                .Text($"{exp.Date:dd/MM/yyyy} | {exp.Description}");

                                            row.ConstantColumn(100)
                                                .AlignRight()
                                                .Text($"R$ {exp.Amount:N2}");
                                        });

                                    odd = !odd;
                                }

                                // pequeno espaço entre tabela e total
                                sec.Item().PaddingTop(4)
                                    .Text($"Total de despesas: R$ {cc.TotalExpenses:N2}")
                                    .FontSize(11)
                                    .Bold();
                            }
                            else
                            {
                                // caixinha sem despesas
                                sec.Item().PaddingTop(4)
                                    .Text("Sem despesas no período.")
                                    .FontSize(10)
                                    .FontColor("#777777");
                            }
                        }

                        // total geral de despesas (salário + caixinhas), maior e verde
                        sec.Item().PaddingTop(18)
                            .Text($"Total de despesas: R$ {totalDespesasGeral:N2}")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#1B5E20");
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