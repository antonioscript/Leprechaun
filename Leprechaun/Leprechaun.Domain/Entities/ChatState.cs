namespace Leprechaun.Domain.Entities;


public class ChatState
{
    public long ChatId { get; set; }
    public string State { get; set; } = "Idle";

    public int? TempInstitutionId { get; set; }
    public decimal? TempAmount { get; set; }

    // Nome temporário da caixinha (fluxo /criar_caixinha)
    public string? TempCostCenterName { get; set; }

    // ?? NOVOS: usados no fluxo /transferir_entre_caixinhas
    public int? TempPersonId { get; set; }
    public int? TempSourceCostCenterId { get; set; }
    public int? TempTargetCostCenterId { get; set; }

    public DateTime UpdatedAt { get; set; }
}