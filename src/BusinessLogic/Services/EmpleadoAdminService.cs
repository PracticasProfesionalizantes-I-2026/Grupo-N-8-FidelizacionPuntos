using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IEmpleadoAdminService"/> (CU-16).</summary>
public class EmpleadoAdminService(IEmpleadoRepository empleadoRepository, IAuditoriaService auditoriaService) : IEmpleadoAdminService
{
    /// <summary>Alta sin contraseña: el empleado la define vía CU-25 (RN-28).</summary>
    public async Task<EmpleadoResponseDTO> CrearAsync(EmpleadoCreateDTO dto)
    {
        if (await empleadoRepository.GetByDocumentoAsync(dto.Documento) is not null
            || await empleadoRepository.GetByEmailAsync(dto.Email) is not null)
        {
            throw new EmpleadoDuplicadoException("Ya existe un empleado con ese documento o email.");
        }

        var empleado = await empleadoRepository.CreateAsync(new Empleado
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("CrearEmpleado", ActorTipo.Admin, null, "Empleado", empleado.Id);
        return MapToResponseDTO(empleado);
    }

    /// <summary>Modifica solo los campos presentes; valida unicidad de email si cambió (RN-01).</summary>
    public async Task<EmpleadoResponseDTO> ActualizarAsync(Guid id, EmpleadoUpdateDTO dto)
    {
        var empleado = await empleadoRepository.GetByIdAsync(id)
            ?? throw new EmpleadoNotFoundException("No se encontró el empleado.");

        if (dto.Email is not null && dto.Email != empleado.Email)
        {
            var otro = await empleadoRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != id)
            {
                throw new EmpleadoDuplicadoException("Ya existe un empleado con ese email.");
            }

            empleado.Email = dto.Email;
        }

        if (dto.Nombre is not null) empleado.Nombre = dto.Nombre;

        await empleadoRepository.UpdateAsync(empleado);
        await auditoriaService.RegistrarAsync("ActualizarEmpleado", ActorTipo.Admin, null, "Empleado", id);
        return MapToResponseDTO(empleado);
    }

    /// <summary>Baja lógica: por RN-15, el empleado pierde el acceso al sistema.</summary>
    public async Task DarDeBajaAsync(Guid id)
    {
        var empleado = await empleadoRepository.GetByIdAsync(id)
            ?? throw new EmpleadoNotFoundException("No se encontró el empleado.");

        empleado.Activo = false;
        await empleadoRepository.UpdateAsync(empleado);
        await auditoriaService.RegistrarAsync("DarDeBajaEmpleado", ActorTipo.Admin, null, "Empleado", id);
    }

    /// <summary>Traduce la entidad al DTO público, sin exponer `PasswordHash`.</summary>
    private static EmpleadoResponseDTO MapToResponseDTO(Empleado e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Documento = e.Documento,
        Email = e.Email,
        Activo = e.Activo,
        FechaAlta = e.FechaAlta
    };
}
