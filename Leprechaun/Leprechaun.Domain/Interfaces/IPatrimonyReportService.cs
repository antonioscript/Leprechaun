using Leprechaun.Domain.Response;

namespace Leprechaun.Domain.Interfaces;

public interface IPatrimonyReportService
{
    // Relatório em texto (usado no Telegram)
    Task<string> BuildPatrimonyReportAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);

    // Dados estruturados para PDF
    Task<PatrimonyReportDto> BuildPatrimonyReportDataAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);
}