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

        if (text.StartsWith("/cancelar", StringComparison.OrdinalIgnoreCase))
            return TelegramCommand.Cancelar;

        return TelegramCommand.Unknown;
    }
}