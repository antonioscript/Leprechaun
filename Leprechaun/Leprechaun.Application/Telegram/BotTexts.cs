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

    "*📊 Relatórios:*\n" +
    "/saldo_salario_acumulado - Mostra o total acumulado e contribuição por titular\n\n" +

    "*💰 Renda:*\n" +
    "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n\n" +

    "*📦 Caixinhas:*\n" +
    "/criar_caixinha - Criar uma nova caixinha\n\n" +
    "/transferir_entre_caixinhas - Transferir valor entre caixinhas do mesmo titular\n\n" +
    "/transferir_sal_acml_para_caixinha - Transferir do salário acumulado para uma caixinha\n\n" +



    "*👤 Titulares:*\n" +
    "/person - Lista dos titulares cadastrados\n\n" +

    "*⚙️ Sistema:*\n" +
    "/start - Mensagem inicial do bot\n" +
    "/help - Lista todos os comandos\n" +
    "/ping - Testa se o bot está online\n" +
    "/cancelar - Cancela o fluxo atual\n";


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