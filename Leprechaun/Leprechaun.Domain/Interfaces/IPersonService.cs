using Leprechaun.Domain.Response;

namespace Leprechaun.Domain.Interfaces;

public interface IPersonService
{
    Task<List<PersonResponse>> GetAllAsync(CancellationToken cancellationToken);
}