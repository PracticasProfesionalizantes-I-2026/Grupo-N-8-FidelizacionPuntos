using FidelixAPI.Shared.DTOs.Empleado;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de empleados por el admin (CU-16).</summary>
public interface IEmpleadoAdminService
{
    Task<EmpleadoResponseDTO> CrearAsync(EmpleadoCreateDTO dto);
    Task<EmpleadoResponseDTO> ActualizarAsync(Guid id, EmpleadoUpdateDTO dto);

    /// <summary>Baja lógica (RN-15): revoca el acceso del empleado.</summary>
    Task DarDeBajaAsync(Guid id);
}
