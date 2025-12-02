using System.Text;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Telegram;

public static class BotTexts
{
    public static string Welcome() =>
        "🍀 Olá! Eu sou o Leprechaun Bot.\n\n" +
        "Comandos disponíveis:\n" +
        "/help - Lista os comandos\n" +
        "/ping - Teste de conexão\n" +
        "/person - Lista os titulares\n" +
        "/cadastrar_salario - Registrar recebimento de salário\n";

    public static string Help() =>
        "📚 *Comandos disponíveis:*\n\n" +
        "/start - Mensagem de boas-vindas\n" +
        "/help - Lista os comandos\n" +
        "/ping - Testa se o bot está online\n" +
        "/person - Lista os titulares da conta\n" +
        "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n";

    public static string UnknownCommand() =>
        "Não entendi 🤔\nUse /help para ver os comandos disponíveis.";

    public static string PersonsList(IEnumerable<PersonResponse> persons)
    {
        var list = persons.ToList();
        if (!list.Any())
            return "Nenhum titular encontrado no banco.";

        var sb = new StringBuilder();
        sb.AppendLine("👥 *Titulares:*\n");
        foreach (var p in list)
            sb.AppendLine($"• {p.Name}");

        return sb.ToString();
    }

    public static string ChooseInstitution(IEnumerable<Institution> institutions)
    {
        var list = institutions.ToList();
        if (!list.Any())
            return "Não há instituições cadastradas.";

        var sb = new StringBuilder();
        sb.AppendLine("🏦 *Escolha a instituição do salário:*\n");
        for (int i = 0; i < list.Count; i++)
            sb.AppendLine($"{i + 1}. {list[i].Name}");

        return sb.ToString();
    }

    public static string AskSalaryAmount(string institutionName) =>
        $"Informe o valor recebido do salário na instituição *{institutionName}*.\n" +
        "Ex: 2560,34";

    public static string SalaryReceipt(
        Institution institution,
        decimal amount,
        DateTime date,
        decimal totalAccumulated)
    {
        return
            "*📄 Comprovante de Recebimento*\n\n" +
            $"🏦 *Instituição:* {institution.Name}\n" +
            $"💰 *Valor:* R$ {amount:N2}\n" +
            $"📅 *Data:* {date:dd/MM/yyyy HH:mm}\n\n" +
            $"💼 *Total Salário Acumulado:* R$ {totalAccumulated:N2}\n\n" +
            "✔ Recebimento registrado com sucesso!";
    }
}