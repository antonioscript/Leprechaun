namespace Leprechaun.Domain.Entities;


public class ChatState
{
    public long ChatId { get; set; }

    // string com os valores de FlowStates
    public string State { get; set; } = "Idle";

    public int? TempInstitutionId { get; set; }
    public decimal? TempAmount { get; set; }

    // ?? NOVO: para /criar_caixinha
    public string? TempCostCenterName { get; set; }

    public DateTime UpdatedAt { get; set; }
}