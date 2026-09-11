using FidelixAPI.Shared.DTOs.Cliente;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de clientes por el admin (CU-15). Ver nota de separación de responsabilidades en <see cref="IClienteService"/>.</summary>
public interface IClienteAdminService
{
    Task<ClienteResponseDTO> CrearAsync(ClienteCreateDTO dto);
    Task<ClienteResponseDTO> ActualizarAsync(Guid id, ClienteUpdateDTO dto);

    /// <summary>Baja lógica (RN-14): 204 No Content, sin cuerpo.</summary>
    Task DarDeBajaAsync(Guid id);
}
