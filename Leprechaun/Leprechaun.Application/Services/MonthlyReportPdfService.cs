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
        var totalDespesasSalario = data.SalaryOutflows;
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
                    row.Spacing(15);

                    // logo maior, alinhada ao centro vertical
                    row.ConstantColumn(90)
                        .AlignMiddle()
                        .Height(70)
                        .Image(logoBytes, ImageScaling.FitArea);

                    // título alinhado verticalmente com a logo
                    row.RelativeColumn()
                        .AlignMiddle()
                        .Column(col =>
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
                    col.Spacing(18);

                    /*
                    // --- Título principal ---
                    col.Item().Text("Relatório de Patrimônio")
                        .FontSize(16)
                        .Bold()
                        .FontColor("#1B5E20");
                    */
                    
                    // --- Visão Geral (estilo Inter) ---
col.Item().Column(sec =>
{
    // Título
    sec.Item().Text("Visão Geral")
        .FontSize(16)
        .Bold()
        .FontColor("#33691E");

    // Espaço entre o título e o card
    sec.Item().Height(8);

    // Card principal
    sec.Item().Element(card =>
    {
        card
            .Border(1)
            .BorderColor("#1B5E20")   // verde Leprechaun
            .Background("#FAFAFA")
            .Padding(16)
            .Row(row =>
            {
                // Coluna esquerda: Saldo
                row.RelativeColumn().Column(left =>
                {
                    left.Item().Text("Saldo")
                        .FontSize(11)
                        .FontColor("#555555");

                    left.Item().Text($"R$ {data.GeneralBalance:N2}")
                        .FontSize(22)
                        .Bold()
                        .FontColor("#1B5E20");
                });

                // Divisor vertical com espaçamento horizontal
                row.ConstantColumn(30)          // dá ~30pt de “respiro” entre saldo e entradas/despesas
                    .BorderLeft(1)
                    .BorderColor("#E0E0E0");

                // Coluna direita: Entradas e Despesas
                row.RelativeColumn().Column(right =>
                {
                    // Entradas
                    right.Item().Row(r =>
                    {
                        r.RelativeColumn()
                            .Text("Entradas")
                            .FontSize(11)
                            .FontColor("#555555");

                        r.ConstantColumn(140)
                            .AlignRight()
                            .Text($"R$ {data.GeneralEntries:N2}")
                            .FontSize(11)
                            .Bold();
                    });

                    right.Item().Height(6);

                    // Despesas
                    right.Item().Row(r =>
                    {
                        r.RelativeColumn()
                            .Text("Despesas")
                            .FontSize(11)
                            .FontColor("#555555");

                        r.ConstantColumn(140)
                            .AlignRight()
                            .Text($"R$ {data.GeneralOutflows:N2}")
                            .FontSize(11);
                    });
                });
            });
    });
});  

                    // --- Salário Acumulado ---
                    col.Item().Column(sec =>
                    {
                        sec.Item().Text("Salário Acumulado")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#33691E");

                        // Removemos as linhas de Entradas / Saídas aqui,
                        // deixando só a lista de despesas + total

                        if (data.SalaryExpenses.Any())
                        {
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
                            // mais espaço antes de cada caixinha
                            sec.Item().PaddingTop(18).Text(cc.Name)
                                .FontSize(13)   // um pouco maior
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

                                // espaço entre tabela e total da caixinha
                                sec.Item().PaddingTop(6)
                                    .Text($"Total de despesas: R$ {cc.TotalExpenses:N2}")
                                    .FontSize(11)
                                    .Bold();
                            }
                            else
                            {
                                sec.Item().PaddingTop(4)
                                    .Text("Sem despesas no período.")
                                    .FontSize(10)
                                    .FontColor("#777777");
                            }
                        }

                        // total geral de despesas, maior e verde
                        sec.Item().PaddingTop(20)
                            .Text($"Total de despesas: R$ {totalDespesasGeral:N2}")
                            .FontSize(15)
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