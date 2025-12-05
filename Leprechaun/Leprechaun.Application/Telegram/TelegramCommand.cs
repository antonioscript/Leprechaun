namespace Leprechaun.Application.Telegram;

public enum TelegramCommand
{
    Unknown = 0,
    Start,
    Help,
    Ping,
    Person,
    CadastrarSalario,
    Cancelar,
    SaldoSalarioAcumulado,
    CriarCaixinha,
    TransferirEntreCaixinhas,
    TransferirSalAcmlParaCaixinha,
    SaldoCaixinhas,
    RegistrarDespesaSalAcml,
    RegistrarDespesaCaixinha,
    ExtratoCaixinhaMes,
    ExtratoSalarioAcumuladoMes,
    SugerirFeature,
    ListarFeatures,
    Patrimonio,
    Imagem,
    Version
}