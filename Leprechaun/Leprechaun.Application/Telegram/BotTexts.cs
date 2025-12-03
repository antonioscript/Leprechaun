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
        "📚 **Comandos disponíveis:**\n\n" +
        "/start - Mensagem de boas-vindas\n" +
        "/help - Lista os comandos\n" +
        "/ping - Testa se o bot está online\n" +
        "/person - Lista os titulares da conta\n" +
        "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n" +
        "/saldo_salario_acumulado - Valor total do salario acumulado\n";

    public static string UnknownCommand() =>
        "Não entendi 🤔\nUse /help para ver os comandos disponíveis.";

    public static string PersonsList(IEnumerable<PersonResponse> persons)
    {
        var list = persons.ToList();
        if (!list.Any())
            return "Nenhum titular encontrado no banco.";

        var sb = new StringBuilder();
        sb.AppendLine("👥 **Titulares:**\n");
        foreach (var p in list)
            sb.AppendLine($"• {p.Name}");

        return sb.ToString();
    }
}