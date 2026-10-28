namespace FidelixAPI.Shared.DTOs.Comun;

/// <summary>Respuesta que solo informa un mensaje (ej.: la recuperación de contraseña, CU-25).</summary>
public class MensajeResponseDTO
{
    public string Mensaje { get; set; } = string.Empty;
}
