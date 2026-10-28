using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Comun;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>Recuperación de contraseña para cualquier actor con credenciales (CU-25).</summary>
public interface IRecuperacionService
{
    /// <summary>
    /// Genera un código de un solo uso con vencimiento corto (RN-27) y lo envía: al propio cliente o
    /// admin, o al admin si la cuenta es de un empleado (RN-28). Siempre responde lo mismo, exista o
    /// no la cuenta, para no revelar qué emails están registrados (flujo 3a).
    /// </summary>
    Task<MensajeResponseDTO> SolicitarRecuperacionAsync(RecuperacionSolicitudDTO dto);

    /// <summary>Valida el código (vigente y sin usar, RN-27) y la nueva contraseña (RN-02), y la actualiza.</summary>
    Task<MensajeResponseDTO> ConfirmarRecuperacionAsync(RecuperacionConfirmacionDTO dto);
}
