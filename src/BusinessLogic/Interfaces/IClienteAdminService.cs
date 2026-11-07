using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de clientes por el admin (CU-15).</summary>
public interface IClienteAdminService
{
    /// <summary>Lista todos los clientes (activos e inactivos) como `{ mensaje, items }`.</summary>
    Task<ListadoResponseDTO<ClienteResponseDTO>> ListarClientesAsync();

    /// <summary>Alta: documento y email únicos (RN-01). La contraseña es opcional (el cliente la define con CU-25).</summary>
    Task<ClienteResponseDTO> CrearClienteAsync(ClienteCreateDTO dto);

    /// <summary>Modificación parcial, incluida la categoría (N1); el email nuevo tiene que ser único (RN-01).</summary>
    Task<ClienteResponseDTO> ActualizarClienteAsync(int id, AdminClienteUpdateDTO dto);

    /// <summary>Baja lógica: el cliente deja de acumular y canjear (RN-14), sin perder su historial.</summary>
    Task BajaClienteAsync(int id);
}
