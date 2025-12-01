namespace Leprechaun.Domain.Response;

public class PersonResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}