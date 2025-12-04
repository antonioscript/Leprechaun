using System.Text;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Telegram;

public static class TextsSalaryIncome
{
    public static string ChooseInstitution(IEnumerable<Institution> institutions)
    {
        var list = institutions.ToList();
        if (!list.Any())
            return "Não há instituições cadastradas.";

        var sb = new StringBuilder();
        sb.AppendLine("🏦 **Escolha a instituição do salário:**\n");
        for (int i = 0; i < list.Count; i++)
            sb.AppendLine($"{i + 1}. {list[i].Name}");

        return sb.ToString();
    }

    public static string AskSalaryAmount(string institutionName) =>
        $"Informe o valor recebido do salário na instituição **{institutionName}**.\n"; 

    public static string SalaryReceipt( Institution institution, decimal amount, DateTime date, decimal totalAccumulated)
    {
        return
            "**📄 Comprovante de Recebimento** \n\n" +
            $"🏦 **Instituição:** {institution.Name}\n" +
            $"💰 **Valor:** R$ {amount:N2}\n" +
            $"📅 **Data:** {date:dd/MM/yyyy HH:mm}\n\n" +
            $"💼 **Total Salário Acumulado:** R$ {totalAccumulated:N2}\n\n" +
            "✔ Recebimento registrado com sucesso!";
    }
}