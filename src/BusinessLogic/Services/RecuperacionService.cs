using System.Security.Cryptography;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Auth;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IRecuperacionService (CU-25). </summary>
public class RecuperacionService(
    IClienteRepository clienteRepository,
    IEmpleadoRepository empleadoRepository,
    IAdminRepository adminRepository,
    ICodigoRecuperacionRepository codigoRepository,
    IEmailSender emailSender) : IRecuperacionService
{
    /// <summary> RN-27: el código vence a los 15 minutos. </summary>
    private const int MinutosVigencia = 15;
    /// <summary> CRIT-04: con 3 códigos equivocados, los códigos vigentes del email se invalidan. </summary>
    public const int MaxIntentosCodigo = 3;
    /// <summary> Mensaje genérico del paso 1: no revela si la cuenta existe (flujo 3a). </summary>
    private const string MensajeGenerico = "Si el dato ingresado corresponde a una cuenta, se envió un código";

    /// <summary>
    /// Busca la cuenta activa por email (cliente, empleado o admin). Si no hay ninguna, responde igual
    /// y no envía nada (3a). Si la hay, guarda el código hasheado y lo envía al destinatario según el rol.
    /// </summary>
    public async Task<MensajeResponseDTO> SolicitarRecuperacionAsync(RecuperacionSolicitudDTO dto)
    {
        var cuenta = await BuscarCuentaActivaAsync(dto.Email);
        if (cuenta is null)
        {
            return new MensajeResponseDTO { Mensaje = MensajeGenerico };
        }

        // Código de 6 dígitos generado con un generador criptográfico (no predecible).
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        await codigoRepository.CreateAsync(new CodigoRecuperacion
        {
            Email = dto.Email,
            Rol = cuenta.Value.Rol,
            CodigoHash = PasswordHasher.Hash(codigo),
            FechaExpiracion = DateTime.UtcNow.AddMinutes(MinutosVigencia),
            Usado = false
        });

        foreach (var destinatario in await DestinatariosAsync(dto.Email, cuenta.Value.Rol))
        {
            await emailSender.EnviarAsync(destinatario, "Fidelix - Código de recuperación",
                $"Código para restablecer la contraseña de {dto.Email}: {codigo} (vence en {MinutosVigencia} minutos).");
        }

        return new MensajeResponseDTO { Mensaje = MensajeGenerico };
    }

    /// <summary>
    /// Valida el código contra los vigentes del email (5a), la nueva contraseña (5b, RN-02), actualiza
    /// la contraseña de la cuenta y marca los códigos como usados para que no se puedan reutilizar (RN-27).
    /// Un código equivocado suma un intento fallido; al tercero los códigos se invalidan (CRIT-04).
    /// </summary>
    public async Task<MensajeResponseDTO> ConfirmarRecuperacionAsync(RecuperacionConfirmacionDTO dto)
    {
        var vigentes = await codigoRepository.GetVigentesPorEmailAsync(dto.Email, DateTime.UtcNow);
        var codigo = vigentes.FirstOrDefault(c => PasswordHasher.Verify(dto.Codigo, c.CodigoHash));

        if (codigo is null)
        {
            await RegistrarIntentoFallidoAsync(vigentes);
            throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró");
        }

        if (!PoliticaPassword.EsValida(dto.NuevaPassword))
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos");
        }

        // Un solo uso: se invalidan todos los códigos vigentes del email, no solo el usado.
        foreach (var c in vigentes)
        {
            c.Usado = true;
        }

        await ActualizarPasswordAsync(dto.Email, codigo.Rol, PasswordHasher.Hash(dto.NuevaPassword));
        await codigoRepository.UpdateRangoAsync(vigentes);

        return new MensajeResponseDTO { Mensaje = "La contraseña se actualizó correctamente." };
    }

    /// <summary>
    /// CRIT-04: suma un intento fallido a cada código vigente del email y, al llegar a
    /// <see cref="MaxIntentosCodigo"/>, lo marca como usado. Así, un código de 6 dígitos (un millón de
    /// combinaciones) no se puede adivinar probando: después de 3 errores hay que pedir uno nuevo, y
    /// pedirlo está limitado por el rate limiting del endpoint.
    /// </summary>
    private async Task RegistrarIntentoFallidoAsync(IReadOnlyList<CodigoRecuperacion> vigentes)
    {
        if (vigentes.Count == 0)
        {
            return;
        }

        foreach (var c in vigentes)
        {
            c.IntentosFallidos++;
            if (c.IntentosFallidos >= MaxIntentosCodigo)
            {
                c.Usado = true;
            }
        }

        await codigoRepository.UpdateRangoAsync(vigentes);
    }

    /// <summary> Busca una cuenta activa con ese email en clientes, empleados y admins, y devuelve su rol. </summary>
    private async Task<(string Rol, int Id)?> BuscarCuentaActivaAsync(string email)
    {
        if (await clienteRepository.GetByEmailAsync(email) is { Activo: true } cliente) return (Roles.Cliente, cliente.Id);
        if (await empleadoRepository.GetByEmailAsync(email) is { Activo: true } empleado) return (Roles.Empleado, empleado.Id);
        if (await adminRepository.GetByEmailAsync(email) is { } admin) return (Roles.Admin, admin.Id);
        return null;
    }

    /// <summary> RN-28: el código de un empleado se envía a los admins; el resto, a la propia cuenta. </summary>
    private async Task<IReadOnlyList<string>> DestinatariosAsync(string email, string rol)
    {
        if (rol != Roles.Empleado)
        {
            return [email];
        }

        var admins = await adminRepository.GetAllAsync();
        return admins.Select(a => a.Email).ToList();
    }

    /// <summary> Guarda el hash nuevo en la tabla que corresponde al rol; al cliente además le quita el bloqueo (RN-03). </summary>
    private async Task ActualizarPasswordAsync(string email, string rol, string nuevoHash)
    {
        switch (rol)
        {
            case Roles.Cliente:
                var cliente = await clienteRepository.GetByEmailAsync(email)
                    ?? throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró");
                cliente.PasswordHash = nuevoHash;
                cliente.IntentosFallidos = 0;
                cliente.BloqueadoHasta = null;
                await clienteRepository.UpdateAsync(cliente);
                break;
            case Roles.Empleado:
                var empleado = await empleadoRepository.GetByEmailAsync(email)
                    ?? throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró");
                empleado.PasswordHash = nuevoHash;
                await empleadoRepository.UpdateAsync(empleado);
                break;
            default:
                var admin = await adminRepository.GetByEmailAsync(email)
                    ?? throw new CodigoRecuperacionInvalidoException("El código ingresado es inválido o expiró");
                admin.PasswordHash = nuevoHash;
                await adminRepository.UpdateAsync(admin);
                break;
        }
    }
}
