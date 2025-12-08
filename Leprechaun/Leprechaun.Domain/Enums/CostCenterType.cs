namespace Leprechaun.Domain.Enums;
public enum CostCenterType
{
    Default = 0,              // caixinha normal
    ProibidaDespesaDireta = 1, // não pode registrar despesa
    InfraMensal = 2           // só pode existir uma
}
