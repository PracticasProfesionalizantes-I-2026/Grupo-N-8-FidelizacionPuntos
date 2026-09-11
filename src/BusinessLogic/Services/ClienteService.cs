using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IClienteService"/> (CU-01, CU-03, CU-12).</summary>
public class ClienteService(IClienteRepository clienteRepository, IAuditoriaService auditoriaService) : IClienteService
{
    /// <summary>Autorregistro: exige contraseña propia y unicidad de documento/email (RN-01).</summary>
    public async Task<ClienteResponseDTO> RegistrarAsync(ClienteCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        await ValidarUnicidadAsync(dto.Documento, dto.Email, idAExcluir: null);

        var cliente = await clienteRepository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("RegistrarCliente", ActorTipo.Cliente, cliente.Id, "Cliente", cliente.Id);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Alta desde POS por un empleado: sin contraseña (se define luego vía CU-25).</summary>
    public async Task<ClienteResponseDTO> CrearDesdePOSAsync(ClienteCreateDTO dto)
    {
        await ValidarUnicidadAsync(dto.Documento, dto.Email, idAExcluir: null);

        var cliente = await clienteRepository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            PasswordHash = null,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("CrearClienteDesdePOS", ActorTipo.Empleado, null, "Cliente", cliente.Id);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Perfil propio.</summary>
    public async Task<ClienteResponseDTO> ObtenerPropioAsync(Guid clienteId)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");
        return MapToResponseDTO(cliente);
    }

    /// <summary>Modifica solo los campos presentes; rechaza cambios al documento (RN-04) y emails duplicados (RN-01).</summary>
    public async Task<ClienteResponseDTO> ActualizarPerfilAsync(Guid clienteId, ClienteUpdateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        if (dto.Documento is not null && dto.Documento != cliente.Documento)
        {
            throw new ValidationException("El documento de identidad no puede ser modificado.");
        }

        if (dto.Email is not null && dto.Email != cliente.Email)
        {
            var otro = await clienteRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != clienteId)
            {
                throw new EmailDuplicadoException("El email ingresado ya se encuentra registrado.");
            }

            cliente.Email = dto.Email;
        }

        if (dto.Nombre is not null) cliente.Nombre = dto.Nombre;
        if (dto.Telefono is not null) cliente.Telefono = dto.Telefono;
        if (dto.Password is not null) cliente.PasswordHash = PasswordHasher.Hash(dto.Password);

        await clienteRepository.UpdateAsync(cliente);
        await auditoriaService.RegistrarAsync("ActualizarPerfilCliente", ActorTipo.Cliente, clienteId, "Cliente", clienteId);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Baja lógica: preserva historial de movimientos y canjes (RN-14).</summary>
    public async Task DarseDeBajaAsync(Guid clienteId)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        cliente.Activo = false;
        await clienteRepository.UpdateAsync(cliente);
        await auditoriaService.RegistrarAsync("DarseDeBajaCliente", ActorTipo.Cliente, clienteId, "Cliente", clienteId);
    }

    /// <summary>Verifica unicidad de documento y email (RN-01) antes de crear un cliente.</summary>
    private async Task ValidarUnicidadAsync(string documento, string email, Guid? idAExcluir)
    {
        var porDocumento = await clienteRepository.GetByDocumentoAsync(documento);
        if (porDocumento is not null && porDocumento.Id != idAExcluir)
        {
            throw new ClienteDuplicadoException("Ya existe un cliente registrado con ese documento.");
        }

        var porEmail = await clienteRepository.GetByEmailAsync(email);
        if (porEmail is not null && porEmail.Id != idAExcluir)
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
