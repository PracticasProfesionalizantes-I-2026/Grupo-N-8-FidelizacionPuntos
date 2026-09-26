using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IClienteService"/> (CU-01, CU-04).</summary>
public class ClienteService(IClienteRepository clienteRepository, IMovimientoRepository movimientoRepository) : IClienteService
{
    /// <summary>Autorregistro: exige contraseña propia (RN-02) y unicidad de documento/email (RN-01).</summary>
    public async Task<ClienteResponseDTO> RegistrarAsync(ClienteCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        await ValidarUnicidadAsync(dto.Documento, dto.Email);

        var cliente = await clienteRepository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        });

        return MapToResponseDTO(cliente);
    }

    /// <summary>
    /// Saldo disponible: suma lo que le queda a cada lote vigente (RN-05). Si
    /// el cliente no existe lanza <see cref="ClienteNotFoundException"/>; si
    /// falla la lectura de datos, <see cref="PersistenceException"/> (flujo 4a).
    /// </summary>
    public async Task<SaldoResponseDTO> ConsultarSaldoAsync(Guid clienteId)
    {
        _ = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        IReadOnlyList<Movimiento> lotes;
        try
        {
            lotes = await movimientoRepository.GetLotesVigentesPorClienteAsync(clienteId, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo obtener el saldo en este momento.", ex);
        }

        return new SaldoResponseDTO { PuntosDisponibles = lotes.Sum(l => l.PuntosDisponibles) };
    }

    /// <summary>Verifica unicidad de documento y email (RN-01) antes de crear un cliente.</summary>
    private async Task ValidarUnicidadAsync(string documento, string email)
    {
        if (await clienteRepository.GetByDocumentoAsync(documento) is not null)
        {
            throw new ClienteDuplicadoException("Ya existe un cliente registrado con ese documento.");
        }

        if (await clienteRepository.GetByEmailAsync(email) is not null)
        {
            throw new ClienteDuplicadoException("Ya existe un cliente registrado con ese email.");
        }
    }

    /// <summary>Traduce la entidad al DTO público, sin exponer `PasswordHash`.</summary>
    private static ClienteResponseDTO MapToResponseDTO(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Documento = c.Documento,
        Email = c.Email,
        Telefono = c.Telefono,
        FechaNacimiento = c.FechaNacimiento,
        Activo = c.Activo,
        FechaRegistro = c.FechaRegistro
    };
}
