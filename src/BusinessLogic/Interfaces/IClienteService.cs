using FidelixAPI.Shared.DTOs.Cliente;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>
/// Ciclo de vida del cliente desde su propia perspectiva y desde el punto de
/// venta (CU-01, CU-03, CU-12). El ABM hecho por el admin (CU-15) vive en
/// <see cref="IClienteAdminService"/>: son reglas de validación distintas
/// (un admin no está sujeto a "no puedo cambiar mi propio documento" de la
/// misma manera que exige CU-03, y sus flujos de error son otros).
/// </summary>
public interface IClienteService
{
    /// <summary>Autorregistro (CU-01): requiere contraseña propia.</summary>
    Task<ClienteResponseDTO> RegistrarAsync(ClienteCreateDTO dto);

    /// <summary>Alta desde el punto de venta por un empleado (CU-12): sin contraseña.</summary>
    Task<ClienteResponseDTO> CrearDesdePOSAsync(ClienteCreateDTO dto);

    /// <summary>Perfil propio (CU-03, GET).</summary>
    Task<ClienteResponseDTO> ObtenerPropioAsync(Guid clienteId);

    /// <summary>Modifica el perfil propio (CU-03, PUT). Rechaza intentos de cambiar el documento (RN-04).</summary>
    Task<ClienteResponseDTO> ActualizarPerfilAsync(Guid clienteId, ClienteUpdateDTO dto);

    /// <summary>Baja voluntaria (CU-03, DELETE): baja lógica, preserva historial (RN-14).</summary>
    Task DarseDeBajaAsync(Guid clienteId);
}
