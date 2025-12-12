namespace Leprechaun.Domain.Interfaces;

public interface IJobService
{
    Task RunJob(string message, CancellationToken cancellationToken = default);
}
