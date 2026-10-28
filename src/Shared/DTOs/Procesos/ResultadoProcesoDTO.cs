namespace FidelixAPI.Shared.DTOs.Procesos;

/// <summary>Resultado de un proceso automático: vencimiento (CU-22) o bono de cumpleaños (CU-26).</summary>
public class ResultadoProcesoDTO
{
    public string Proceso { get; set; } = string.Empty;

    /// <summary>"Exito", "SinCambios" o "ErrorParcial".</summary>
    public string Estado { get; set; } = string.Empty;

    public int ClientesProcesados { get; set; }
    public int ClientesConError { get; set; }
    public int PuntosAfectados { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
