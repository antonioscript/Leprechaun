using Leprechaun.Domain.Response;

namespace Leprechaun.Domain.Interfaces;

public interface IMonthlyReportPdfService
{
    byte[] GeneratePatrimonyReportPdf(string title, PatrimonyReportDto data);
}