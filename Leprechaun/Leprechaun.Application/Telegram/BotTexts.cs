using System.Text;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Telegram;

public static class BotTexts
{
    public static string Start() =>
    "🍀 Olá! Eu sou o Leprechaun Bot.\n\n" +
    "📚 *Comandos disponíveis:*\n\n" +

    "*📊 Relatórios:*\n" +
    "/saldo_salario_acumulado - Mostra o total acumulado e divisão por titular\n\n" +
    "/extrato_salario_acumulado_mes - Extrato mensal das saídas do salário acumulado\n\n" +
    "/saldo_caixinhas - Mostra o saldo das caixinhas por titular\n\n" +
    "/extrato_caixinha_mes - Extrato de despesas da caixinha no mês atual\n\n" +
        

    "*💵 Salário Acumulado:*\n" +
    "/transferir_sal_acml_para_caixinha - Transferir do salário acumulado para uma caixinha\n\n" +
    "/registrar_despesa_sal_acml - Registrar uma despesa que sai do salário acumulado\n\n" +


    "*📦 Caixinhas:*\n" +
    "/criar_caixinha - Criar uma nova caixinha\n\n" +
    "/transferir_entre_caixinhas - Transferir valor entre caixinhas do mesmo titular\n\n" +
    "/registrar_despesa_caixinha - Registrar uma despesa retirada de uma caixinha\n\n" +


    "*💰 Renda:*\n" +
    "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n\n" +


    "*⚙️ Sistema:*\n" +
    "/start - Mensagem inicial do bot\n" +
    "/ping - Testa se o bot está online\n" +
    "/cancelar - Cancela o fluxo atual\n";


    public static string UnknownCommand() =>
        "Não entendi 🤔\nUse /help para ver os comandos disponíveis.";

}