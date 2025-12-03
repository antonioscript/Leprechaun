namespace Leprechaun.Domain.Response;

public class SalaryAccumulatedByPersonResponse
{
    public int PersonId { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}