namespace FidelixAPI.Shared.Enums;

/// <summary>Estado de entrega de un canje, visible para el cliente y el empleado (RN-10).</summary>
public enum EstadoCanje
{
    /// <summary>Canje hecho por el cliente desde la app (CU-07): falta que retire el beneficio.</summary>
    Pendiente,

    /// <summary>Canje presencial (CU-11): el empleado entrega el beneficio en el momento.</summary>
    Entregado
}
