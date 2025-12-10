namespace Leprechaun.Domain.Interfaces;

public interface IMonthlyReportPdfService
{
    byte[] GeneratePatrimonyReportPdf(string title, string reportText);
}