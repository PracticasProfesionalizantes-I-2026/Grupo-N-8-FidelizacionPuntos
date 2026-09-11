using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.Configuration;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>
/// Implementación de <see cref="IAuthService"/>: login unificado en cascada
/// (CU-02, CU-09, CU-14) y recuperación de contraseña (CU-25).
/// </summary>
public class AuthService(
    IClienteRepository clienteRepository,
    IEmpleadoRepository empleadoRepository,
    IAdminRepository adminRepository,
    ICodigoRecuperacionRepository codigoRecuperacionRepository,
    IEmailSender emailSender,
    IAuditoriaService auditoriaService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const int MaxIntentosFallidos = 5; // RN-03: umbral no especificado por la CU; valor por defecto razonable.
    private const int DuracionBloqueoMinutos = 15;
    private const int VigenciaCodigoMinutos = 15; // RN-27: vencimiento corto.

    /// <summary>Prueba las credenciales Cliente → Empleado → Admin y emite el JWT del primero que matchea.</summary>
    public async Task<LoginResponseDTO> LoginAsync(LoginDTO dto)
    {
        var cliente = await clienteRepository.GetByEmailAsync(dto.Email);
        if (cliente is not null)
        {
            return await IntentarLoginAsync(cliente, dto.Password, RolUsuario.Cliente, clienteRepository.UpdateAsync);
        }

        var empleado = await empleadoRepository.GetByEmailAsync(dto.Email);
        if (empleado is not null)
        {
            return await IntentarLoginAsync(empleado, dto.Password, RolUsuario.Empleado, empleadoRepository.UpdateAsync);
        }

        var admin = await adminRepository.GetByEmailAsync(dto.Email);
        if (admin is not null)
        {
            return await IntentarLoginAsync(admin, dto.Password, RolUsuario.Admin, adminRepository.UpdateAsync);
        }

        throw new CredencialesInvalidasException("Email o contraseña incorrectos.");
    }

    /// <summary>
    /// Valida bloqueo (RN-03), estado activo (RN-14/RN-15) y contraseña para
    /// una cuenta ya localizada, y emite el JWT si todo es correcto. Genérico
    /// sobre el tipo de cuenta para no triplicar esta lógica entre Cliente,
    /// Empleado y Admin.
    /// </summary>
    private async Task<LoginResponseDTO> IntentarLoginAsync<TCuenta>(
        TCuenta cuenta, string password, RolUsuario rol, Func<TCuenta, Task> persistirAsync)
        where TCuenta : CuentaConCredenciales
    {
        if (cuenta.BloqueadoHasta is { } bloqueadoHasta && bloqueadoHasta > DateTime.UtcNow)
        {
            throw new CuentaBloqueadaException("La cuenta está bloqueada temporalmente por intentos fallidos.");
        }

        if (!cuenta.Activo)
        {
            throw new CuentaInactivaException("La cuenta está dada de baja.");
        }

        if (cuenta.PasswordHash is null || !PasswordHasher.Verify(password, cuenta.PasswordHash))
        {
            cuenta.IntentosFallidos++;
            if (cuenta.IntentosFallidos >= MaxIntentosFallidos)
            {
                cuenta.BloqueadoHasta = DateTime.UtcNow.AddMinutes(DuracionBloqueoMinutos);
                cuenta.IntentosFallidos = 0;
            }

            await persistirAsync(cuenta);
            throw new CredencialesInvalidasException("Email o contraseña incorrectos.");
        }

        cuenta.IntentosFallidos = 0;
        await persistirAsync(cuenta);

        return new LoginResponseDTO
        {
            Token = GenerarToken(cuenta.Id, rol),
            Rol = rol,
            ExpiraEn = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes)
        };
    }

    /// <summary>Firma un JWT con el Id y el rol de la cuenta autenticada.</summary>
    private string GenerarToken(Guid id, RolUsuario rol)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, rol.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Genera y envía el código de recuperación (CU-25, paso 1); nunca revela si la cuenta existe (RN-27).</summary>
    public async Task SolicitarRecuperacionAsync(RecuperarContrasenaDTO dto)
    {
        var cliente = await BuscarClientePorIdentificadorAsync(dto.Identificador);
        if (cliente is { Activo: true })
        {
            await EmitirYEnviarCodigoAsync(cliente.Email, clienteId: cliente.Id, empleadoId: null, adminId: null);
            return;
        }

        var empleado = await BuscarEmpleadoPorIdentificadorAsync(dto.Identificador);
        if (empleado is { Activo: true })
        {
            // RN-28: el código del empleado se envía al admin, no al empleado.
            var adminDestino = await adminRepository.GetPrimerActivoAsync();
            if (adminDestino is not null)
            {
                await EmitirYEnviarCodigoAsync(adminDestino.Email, clienteId: null, empleadoId: empleado.Id, adminId: null);
            }

            return;
        }

        var admin = await adminRepository.GetByEmailAsync(dto.Identificador);
        if (admin is { Activo: true })
        {
            await EmitirYEnviarCodigoAsync(admin.Email, clienteId: null, empleadoId: null, adminId: admin.Id);
        }

        // Ninguna cuenta encontrada: no se envía nada y no se informa (RN-27).
    }

    /// <summary>Valida el código contra la cuenta correspondiente y actualiza la contraseña (CU-25, paso 2).</summary>
    public async Task ConfirmarRecuperacionAsync(ConfirmarRecuperacionDTO dto)
    {
        if (dto.NuevaPassword.Length < 8)
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        var cliente = await BuscarClientePorIdentificadorAsync(dto.Identificador);
        if (cliente is not null)
        {
            var vigentes = await codigoRecuperacionRepository.GetVigentesPorClienteAsync(cliente.Id, DateTime.UtcNow);
            await AplicarCambioDeContrasenaAsync(dto, vigentes, cliente, clienteRepository.UpdateAsync);
            return;
        }

        var empleado = await BuscarEmpleadoPorIdentificadorAsync(dto.Identificador);
        if (empleado is not null)
        {
            var vigentes = await codigoRecuperacionRepository.GetVigentesPorEmpleadoAsync(empleado.Id, DateTime.UtcNow);
            await AplicarCambioDeContrasenaAsync(dto, vigentes, empleado, empleadoRepository.UpdateAsync);
            return;
        }

        var admin = await adminRepository.GetByEmailAsync(dto.Identificador);
        if (admin is not null)
        {
            var vigentes = await codigoRecuperacionRepository.GetVigentesPorAdminAsync(admin.Id, DateTime.UtcNow);
            await AplicarCambioDeContrasenaAsync(dto, vigentes, admin, adminRepository.UpdateAsync);
            return;
        }

        // Identificador sin cuenta asociada: mismo error que un código inválido, sin distinguir el caso (RN-27).
        throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró.");
    }

    /// <summary>Verifica el código contra los vigentes, y si matchea, actualiza la contraseña y marca el código usado.</summary>
    private async Task AplicarCambioDeContrasenaAsync<TCuenta>(
        ConfirmarRecuperacionDTO dto, IReadOnlyList<CodigoRecuperacion> vigentes, TCuenta cuenta, Func<TCuenta, Task> persistirCuentaAsync)
        where TCuenta : CuentaConCredenciales
    {
        var codigo = vigentes.FirstOrDefault(c => PasswordHasher.Verify(dto.Codigo, c.CodigoHash))
            ?? throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró.");

        codigo.Usado = true;
        await codigoRecuperacionRepository.UpdateAsync(codigo);

        cuenta.PasswordHash = PasswordHasher.Hash(dto.NuevaPassword);
        cuenta.IntentosFallidos = 0;
        cuenta.BloqueadoHasta = null;
        await persistirCuentaAsync(cuenta);
    }

    /// <summary>Genera el código, lo persiste hasheado y lo envía por email (RN-27).</summary>
    private async Task EmitirYEnviarCodigoAsync(string destinatarioEmail, Guid? clienteId, Guid? empleadoId, Guid? adminId)
    {
        var codigo = Random.Shared.Next(0, 1_000_000).ToString("D6");

        await codigoRecuperacionRepository.CreateAsync(new CodigoRecuperacion
        {
            ClienteId = clienteId,
            EmpleadoId = empleadoId,
            AdminId = adminId,
            CodigoHash = PasswordHasher.Hash(codigo),
            FechaExpiracion = DateTime.UtcNow.AddMinutes(VigenciaCodigoMinutos)
        });

        await emailSender.EnviarAsync(destinatarioEmail, "Código de recuperación de contraseña - Fidelix",
            $"Tu código de recuperación es {codigo}. Vence en {VigenciaCodigoMinutos} minutos.");

        await auditoriaService.RegistrarAsync("SolicitarRecuperacionContrasena", ActorTipo.Sistema, null,
            clienteId is not null ? "Cliente" : empleadoId is not null ? "Empleado" : "Admin",
            clienteId ?? empleadoId ?? adminId);
    }

    private Task<Cliente?> BuscarClientePorIdentificadorAsync(string identificador) =>
        BuscarPorDocumentoOEmailAsync(clienteRepository.GetByDocumentoAsync, clienteRepository.GetByEmailAsync, identificador);

    private Task<Empleado?> BuscarEmpleadoPorIdentificadorAsync(string identificador) =>
        BuscarPorDocumentoOEmailAsync(empleadoRepository.GetByDocumentoAsync, empleadoRepository.GetByEmailAsync, identificador);

    /// <summary>El identificador de CU-25 puede ser documento o email: se prueban ambos criterios.</summary>
    private static async Task<TCuenta?> BuscarPorDocumentoOEmailAsync<TCuenta>(
        Func<string, Task<TCuenta?>> buscarPorDocumentoAsync, Func<string, Task<TCuenta?>> buscarPorEmailAsync, string identificador)
        where TCuenta : class =>
        await buscarPorDocumentoAsync(identificador) ?? await buscarPorEmailAsync(identificador);
}
