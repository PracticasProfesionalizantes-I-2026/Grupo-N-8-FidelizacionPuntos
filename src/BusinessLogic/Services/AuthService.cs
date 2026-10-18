using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>
/// Implementación del servicio de Autenticación.
/// CU-02, Flujo Principal y flujos alternativos 3a/3b/3c y RN-03
/// </summary>
public class AuthService(ITokenService tokenService, IClienteRepository clienteRepository) : IAuthService
{
    /// <summary> Umbral de intentos fallidos para bloquear la cuenta </summary>
    private const int UmbralIntentos = 3;
    /// <summary> Tiempo base de espera cuando se bloquea la cuenta </summary>
    private const int EsperaBaseSegundos = 30;
    /// <summary> Tiempo máximo de espera cuando se bloquea la cuenta </summary>
    private const int EsperaMaximaSegundos = 900;
    /// <summary> Rol que se incluye en el token de un cliente. </summary>
    private const string RolCliente = "Cliente";

    /// <summary>
    /// Metodo para validar las credenciales del usuario y generar el token de autenticación
    /// Resetea el cocntador de intentos fallidos en caso de exito.
    /// Aplica RN-03
    /// </summary>
    public async Task<LoginResponseDTO> LoginClienteAsync(LoginRequestDTO dto)
    {
        // 1. Buscar el cliente por email -> si no existe: CredencialesInvalidasException (3a)
        var clienteEncontrado = await clienteRepository.GetByEmailAsync(dto.Email);
        if (clienteEncontrado == null)
        {
            throw new CredencialesInvalidasException("Usuario o contraseña incorrectos");
        }

        // 2. Validar si esa cuenta esta activa -> si no esta activa: CuentaInactivaException (3c)
        if (!clienteEncontrado.Activo)
        {
            throw new CuentaInactivaException("La cuenta se encuentra inactiva. Contacte al administrador");
        }

        // 3. Verificar si esta bloqueado -> Si esta bloqueada: CuentaBloqueadaException (3b)
        if (clienteEncontrado.BloqueadoHasta > DateTime.UtcNow)
        {
            throw new CuentaBloqueadaException("La cuenta se encuentra bloqueada temporalmente. Intente nuevamente más tarde");
        }

        // 4. Verificar si la contrasenia coincide -> Si no coincide: sumar un intento, aplicar RN-03 (Bloqueada?) guardar y lanzar exception 3a
        if (clienteEncontrado.PasswordHash == null || !PasswordHasher.Verify(dto.Password, clienteEncontrado.PasswordHash))
        {
            clienteEncontrado.IntentosFallidos++;
            if (clienteEncontrado.IntentosFallidos >= UmbralIntentos)
            {
                int exponente = clienteEncontrado.IntentosFallidos - UmbralIntentos;
                var espera = TimeSpan.FromSeconds(Math.Min(EsperaMaximaSegundos, EsperaBaseSegundos * Math.Pow(2, exponente)));
                clienteEncontrado.BloqueadoHasta = DateTime.UtcNow.Add(espera);
            }
            await clienteRepository.UpdateAsync(clienteEncontrado);
            throw new CredencialesInvalidasException("Usuario o contraseña incorrectos");
        }

        // 5. Contador en 0, guarda y genera token y devuelve el objeto -> 200 OK
        clienteEncontrado.IntentosFallidos = 0;
        clienteEncontrado.BloqueadoHasta = null;
        await clienteRepository.UpdateAsync(clienteEncontrado);

        var token = tokenService.GenerarToken(clienteEncontrado.Id, RolCliente);

        return MapToLoginResponseDTO(clienteEncontrado, token);
    }

    /// <summary> Traduce la entidad al DTO publico, sin exponer 'PasswordHash' </summary>
    private static LoginResponseDTO MapToLoginResponseDTO(Cliente c, string token) => new()
    {
        Token = token,
        Cliente = new ClienteResponseDTO()
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Documento = c.Documento,
            Email = c.Email,
            Telefono = c.Telefono,
            FechaNacimiento = c.FechaNacimiento,
            Activo = c.Activo,
            FechaRegistro = c.FechaRegistro,
            Roles = new string[] { RolCliente }
        }
    };
}