using FidelixAPI.Shared.DTOs.Empleado;

namespace FidelixAPI.BusinessLogic.Interfaces;

/// <summary>ABM de empleados por el admin (CU-16).</summary>
public interface IEmpleadoAdminService
{
    /// <summary>Lista todos los empleados (activos e inactivos).</summary>
    Task<IReadOnlyList<EmpleadoResponseDTO>> ListarEmpleadosAsync();

    /// <summary>Alta: documento y email únicos (RN-01) y contraseña según la política (RN-02).</summary>
    Task<EmpleadoResponseDTO> CrearEmpleadoAsync(EmpleadoCreateDTO dto);

    /// <summary>Modificación parcial; el email nuevo tiene que seguir siendo único (RN-01).</summary>
    Task<EmpleadoResponseDTO> ActualizarEmpleadoAsync(Guid id, EmpleadoUpdateDTO dto);

    /// <summary>Baja lógica: el empleado pierde el acceso (RN-15).</summary>
    Task BajaEmpleadoAsync(Guid id);
}
