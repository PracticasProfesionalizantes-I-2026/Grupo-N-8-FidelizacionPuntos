using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IEmpleadoAdminService (CU-16). </summary>
public class EmpleadoAdminService(IEmpleadoRepository empleadoRepository) : IEmpleadoAdminService
{
    /// <summary> Lista todos los empleados. </summary>
    public async Task<IReadOnlyList<EmpleadoResponseDTO>> ListarEmpleadosAsync()
    {
        var empleados = await empleadoRepository.GetAllAsync();
        return empleados.Select(MapToResponseDTO).ToList();
    }

    /// <summary> Alta: contraseña válida (RN-02) y documento/email libres (RN-01, flujo 3a). </summary>
    public async Task<EmpleadoResponseDTO> CrearEmpleadoAsync(EmpleadoCreateDTO dto)
    {
        if (!PoliticaPassword.EsValida(dto.Password))
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        if (await empleadoRepository.GetByDocumentoAsync(dto.Documento) is not null)
        {
            throw new EmpleadoDuplicadoException("El documento ingresado pertenece a un empleado activo");
        }

        if (await empleadoRepository.GetByEmailAsync(dto.Email) is not null)
        {
            throw new EmpleadoDuplicadoException("El email ingresado ya pertenece a un empleado");
        }

        var empleado = await empleadoRepository.CreateAsync(new Empleado
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Activo = true,
            FechaAlta = DateTime.UtcNow
        });

        return MapToResponseDTO(empleado);
    }

    /// <summary> Modificación parcial: 404 si no existe (3b), email único si cambia y contraseña válida si viene. </summary>
    public async Task<EmpleadoResponseDTO> ActualizarEmpleadoAsync(Guid id, EmpleadoUpdateDTO dto)
    {
        var empleado = await empleadoRepository.GetByIdAsync(id)
            ?? throw new EmpleadoNotFoundException("El empleado no existe.");

        if (dto.Password is not null && !PoliticaPassword.EsValida(dto.Password))
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        if (dto.Email is not null && dto.Email != empleado.Email)
        {
            var otro = await empleadoRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != id)
            {
                throw new EmpleadoDuplicadoException("El email ingresado ya pertenece a un empleado");
            }

            empleado.Email = dto.Email;
        }

        if (dto.Nombre is not null) empleado.Nombre = dto.Nombre;
        if (dto.Password is not null) empleado.PasswordHash = PasswordHasher.Hash(dto.Password);

        await empleadoRepository.UpdateAsync(empleado);
        return MapToResponseDTO(empleado);
    }

    /// <summary> Baja lógica (RN-15): el login lo rechaza a partir de ahora. </summary>
    public async Task BajaEmpleadoAsync(Guid id)
    {
        var empleado = await empleadoRepository.GetByIdAsync(id)
            ?? throw new EmpleadoNotFoundException("El empleado no existe.");

        empleado.Activo = false;
        await empleadoRepository.UpdateAsync(empleado);
    }

    /// <summary> Traduce la entidad al DTO público, sin el hash de la contraseña. </summary>
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
