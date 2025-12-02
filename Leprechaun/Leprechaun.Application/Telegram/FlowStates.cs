namespace Leprechaun.Application.Telegram;

public static class FlowStates
{
    public const string Idle = "Idle";

    // Fluxo de cadastro de salário
    public const string SalaryAwaitingInstitution = "Salary_AwaitingInstitution";
    public const string SalaryAwaitingAmount = "Salary_AwaitingAmount";
}