using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IAuthPersonalService (CU-09, CU-14). </summary>
public class AuthPersonalService(
    ITokenService tokenService,
    IEmpleadoRepository empleadoRepository,
    IAdminRepository adminRepository) : IAuthPersonalService
{
    /// <summary> Mensaje genérico: no revela si falló el email o la contraseña (flujo 3a). </summary>
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos";

    /// <summary> CU-09: existe y la contraseña coincide (3a), está activo (3b, RN-15), y genera el token. </summary>
    public async Task<LoginPersonalResponseDTO> LoginEmpleadoAsync(LoginRequestDTO dto)
    {
        var empleado = await empleadoRepository.GetByEmailAsync(dto.Email);
        if (empleado is null || !PasswordHasher.Verify(dto.Password, empleado.PasswordHash))
        {
            throw new CredencialesInvalidasException(MensajeCredenciales);
        }

        if (!empleado.Activo)
        {
            throw new CuentaInactivaException("La cuenta se encuentra inactiva. Contacte al administrador");
        }

        return new LoginPersonalResponseDTO
        {
            Token = tokenService.GenerarToken(empleado.Id, Roles.Empleado),
            Id = empleado.Id,
            Nombre = empleado.Nombre,
            Email = empleado.Email,
            Rol = Roles.Empleado
        };
    }

    /// <summary> CU-14: existe y la contraseña coincide (3a), y genera el token con rol Admin. </summary>
    public async Task<LoginPersonalResponseDTO> LoginAdminAsync(LoginRequestDTO dto)
    {
        var admin = await adminRepository.GetByEmailAsync(dto.Email);
        if (admin is null || !PasswordHasher.Verify(dto.Password, admin.PasswordHash))
        {
            throw new CredencialesInvalidasException(MensajeCredenciales);
        }

        return new LoginPersonalResponseDTO
        {
            Token = tokenService.GenerarToken(admin.Id, Roles.Admin),
            Id = admin.Id,
            Nombre = admin.Nombre,
            Email = admin.Email,
            Rol = Roles.Admin
        };
    }
}
