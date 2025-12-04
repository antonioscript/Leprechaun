namespace Leprechaun.Application.Telegram;

public static class FlowStates
{
    public const string Idle = "Idle";

    // Fluxo de cadastro de salário
    public const string SalaryAwaitingInstitution = "Salary_AwaitingInstitution";
    public const string SalaryAwaitingAmount = "Salary_AwaitingAmount";

    // Fluxo de criação de caixinha
    public const string CostCenterAwaitingName = "CostCenter_AwaitingName";
    public const string CostCenterAwaitingOwner = "CostCenter_AwaitingOwner";

    // Fluxo de transferência entre caixinhas
    public const string CostCenterTransferAwaitingPerson = "CostCenterTransfer_AwaitingPerson";
    public const string CostCenterTransferAwaitingSource = "CostCenterTransfer_AwaitingSource";
    public const string CostCenterTransferAwaitingTarget = "CostCenterTransfer_AwaitingTarget";
    public const string CostCenterTransferAwaitingAmount = "CostCenterTransfer_AwaitingAmount";

    // Fluxo de transferir do salário acumulado para caixinha
    public const string SalaryToCostCenterAwaitingPerson = "SalaryToCostCenter_AwaitingPerson";
    public const string SalaryToCostCenterAwaitingTarget = "SalaryToCostCenter_AwaitingTarget";
    public const string SalaryToCostCenterAwaitingAmount = "SalaryToCostCenter_AwaitingAmount";
}
