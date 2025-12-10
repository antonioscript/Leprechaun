namespace Leprechaun.Domain.Interfaces;


public interface IPatrimonyReportService
{
    Task<string> BuildPatrimonyReportAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);
}