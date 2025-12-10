using System.Text;
using Leprechaun.Domain.Entities;
using Leprechaun.Domain.Response;

namespace Leprechaun.Application.Telegram;

public static class BotTexts
{
    public static string Start() =>
    "🍀 Olá! Eu sou o Leprechaun Bot.\n\n" +
    "📚 Comandos disponíveis:\n\n" +

    "📊 Relatórios:\n" +
    "/patrimonio - Mostra o patrimônio total (salário acumulado + caixinhas)\n\n" +
    "/saldo_salario_acumulado - Mostra o total acumulado e divisão por titular\n\n" +
    "/extrato_salario_acumulado_mes - Extrato mensal das saídas do salário acumulado\n\n" +
    "/saldo_caixinhas - Mostra o saldo das caixinhas por titular\n\n" +
    "/extrato_caixinha_mes - Extrato de despesas da caixinha no mês atual\n\n" +
        

    "💵 Salário Acumulado:\n" +
    "/transferir_sal_acml_para_caixinha - Transferir do salário acumulado para uma caixinha\n\n" +
    "/registrar_despesa_sal_acml - Registrar uma despesa que sai do salário acumulado\n\n" +


    "📦 Caixinhas:\n" +
    "/criar_caixinha - Criar uma nova caixinha\n\n" +
    "/transferir_entre_caixinhas - Transferir valor entre caixinhas do mesmo titular\n\n" +
    "/registrar_despesa_caixinha - Registrar uma despesa retirada de uma caixinha\n\n" +


    "💰 Renda:\n" +
    "/cadastrar_salario - Fluxo para registrar o recebimento do salário\n\n" +
    "/saldo_salarios - Visualiza todas as entradas de salários no mês\n\n" +


    "📢 Suporte:\n" +
    "/sugerir_feature - Sugesrir Ideias de Novas Features\n\n" +
    "/listar_features - Listar Features Cadastradas\n\n" +

    "⚙️ Sistema:\n" +
    "/start - Mensagem inicial do bot\n" +
    "/ping - Testa se o bot está online\n" +
    "/cancelar - Cancela o fluxo atual\n";


    public static string HintSeeCostCenterReports() =>
       "💡 Para ver mais informações, você pode usar:\n\n" +
       "/saldo_caixinhas - Ver o saldo das caixinhas\n" +
       "/extrato_caixinha_mes - Ver o extrato das caixinhas no mês atual\n";


    // 🔹 NOVO: dica específica após registrar despesa na caixinha
    public static string HintAfterCostCenterExpense() =>
        "💡 Para continuar, você pode:\n\n" +
        "/registrar_despesa_caixinha - Registrar outra despesa na caixinha\n" +
        "/extrato_caixinha_mes - Ver o extrato das despesas da caixinha no mês atual\n";

    public static string HintAfterCreateCostCenter() =>
        "💡 Agora que a caixinha foi criada, você pode:\n\n" +
        "/transferir_sal_acml_para_caixinha - Transferir dinheiro do salário acumulado para a nova caixinha\n" +
        "/transferir_entre_caixinhas - Transferir valor entre caixinhas\n" +
        "/saldo_caixinhas - Ver o saldo das caixinhas\n";

    public static string HintAfterSalaryExpense() =>
       "💡 Para registrar outra despesa do salário acumulado, você pode usar:\n\n" +
       "/registrar_despesa_sal_acml - Registrar outra despesa do salário acumulado\n";

    public static string HintAfterTransferBetweenCostCenters() =>
        "💡 Para acompanhar suas caixinhas, você pode:\n\n" +
        "/saldo_caixinhas - Ver o saldo das caixinhas\n" +
        "/extrato_caixinha_mes - Ver o extrato das caixinhas no mês atual\n" +
        "/transferir_entre_caixinhas - Fazer outra transferência entre caixinhas\n";

    public static string HintAfterSuggestion(long id) =>
    $"🎉 Obrigado pela sua sugestão! \n\n" +
    $"📝 Ela foi registrada com o código: #{id}\n" +
    $"💾 Agora ela já faz parte da lista de melhorias do Leprechaun.\n\n" +
    $"Se quiser continuar contribuindo:\n" +
    $"• Envie outra sugestão usando /sugerir_feature\n" +
    $"• Veja todas as sugestões com /listar_features\n\n" +
    $"🍀 Obrigado por ajudar o Leprechaun Finance a ficar cada vez melhor!";


    public static string FormatSuggestionListHeader() =>
        "📝 Últimas sugestões registradas:\n";

    public static string NoSuggestions() =>
        "Ainda não há sugestões registradas.";


    public static string VersionNote() =>
    "🟩 Release Notes — Versão 1.2.1\n\n" +
    "Novas Features 🚀\n" +
    "• Atualização: Meta alterada para R$ 500.000, 00\n" +
    "• Atualização: As Caixinhas agora tem 3 tipos (Default, Proibida Despesa Direta e Infra Mensal).\n" +
    "• Nova regra: Não se pode cadastrar uma despesa de uma caixinha que foi marcada como 'Proibida Despesa Direta' .\n" +
    "• Nova Funcionalidade: Nas despesas mensais de Infra aparece a lista de despesa pré-cadastradas.\n" +
    "• Nova Funcionalidade: No relatório da Caixinha de Infra Mensal apresenta particularidades diferentes comparadas com outras caixinhas .\n" +
    "• Novo comando /saldo_salarios para visualizar todas os recebimentos de salário no mês.\n\n" +
    "Versão: 1.2.1\n" +
    "— Leprechaun Bot";



    public static string UnknownCommand() =>
        "Não entendi 🤔\nUse /help para ver os comandos disponíveis.";

    public static string Production() =>
    "🍀 Bem-vindo ao Leprechaun Finance! (o melhor da Vila Leprechaun haha) \n\n" +
    "Seu assistente pessoal para organização financeira chegou! \n\n" +
    "Comigo você pode:\n" +
    "• Registrar salários e entradas de renda\n" +
    "• Controlar caixinhas individuais para cada objetivo\n" +
    "• Acompanhar extratos mensais\n" +
    "• Transferir valores entre caixinhas\n" +
    "• Registrar despesas do salário acumulado\n" +
    "• Enviar sugestões de melhoria diretamente aqui\n\n" +
    "Tudo isso de forma simples, rápida e totalmente integrada ao seu sistema financeiro. \n\n" +
    "📌 Para começar, use o comando /start.\n" +
    "💼 Vamos construir sua liberdade financeira passo a passo. Conte comigo! 🍀";


}