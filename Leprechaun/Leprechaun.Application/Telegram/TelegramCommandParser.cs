namespace Leprechaun.Application.Telegram;

public static class TelegramCommandParser
{
    public static TelegramCommand Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TelegramCommand.Unknown;

        text = text.Trim();

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Start;

        if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Help;

        if (text.StartsWith("/ping", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Ping;

        if (text.StartsWith("/person", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Person;

        if (text.StartsWith("/cadastrar_salario", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.CadastrarSalario;

        if (text.StartsWith("/saldo_salario_acumulado", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.SaldoSalarioAcumulado;

        if (text.StartsWith("/criar_caixinha", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.CriarCaixinha;

        if (text.StartsWith("/transferir_entre_caixinhas", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.TransferirEntreCaixinhas;

        if (text.StartsWith("/transferir_sal_acml_para_caixinha", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.TransferirSalAcmlParaCaixinha;

        if (text.StartsWith("/saldo_caixinhas", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.SaldoCaixinhas;

        if (text.StartsWith("/registrar_despesa_sal_acml", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.RegistrarDespesaSalAcml;
        
        if (text.StartsWith("/registrar_despesa_caixinha", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.RegistrarDespesaCaixinha;

        if (text.StartsWith("/extrato_caixinha_mes", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.ExtratoCaixinhaMes;

        if (text.StartsWith("/extrato_salario_acumulado_mes", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.ExtratoSalarioAcumuladoMes;

        if (text.StartsWith("/sugerir_feature", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.SugerirFeature;

        if (text.StartsWith("/listar_features", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.ListarFeatures;

        if (text.StartsWith("/patrimonio", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Patrimonio;

        if (text.StartsWith("/saldo_salarios", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.SaldoSalarios;

        if (text.StartsWith("/imagem", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Imagem;

        if (text.StartsWith("/cancelar", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Cancelar;

        if (text.StartsWith("/version", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Version;

        return TelegramCommand.Unknown;
    }
}