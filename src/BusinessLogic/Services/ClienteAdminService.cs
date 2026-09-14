using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IClienteAdminService"/> (CU-15).</summary>
public class ClienteAdminService(IClienteRepository clienteRepository, IAuditoriaService auditoriaService) : IClienteAdminService
{
    /// <summary>Alta desde el panel de administración (RN-01, unicidad de documento/email).</summary>
    public async Task<ClienteResponseDTO> CrearAsync(ClienteCreateDTO dto)
    {
        if (await clienteRepository.GetByDocumentoAsync(dto.Documento) is not null
            || await clienteRepository.GetByEmailAsync(dto.Email) is not null)
        {
            throw new ClienteDuplicadoException("Ya existe un cliente con ese documento o email.");
        }

        var cliente = await clienteRepository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        });

        await auditoriaService.RegistrarAsync("CrearClienteAdmin", ActorTipo.Admin, null, "Cliente", cliente.Id);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Modifica los campos presentes de un cliente existente.</summary>
    public async Task<ClienteResponseDTO> ActualizarAsync(Guid id, ClienteUpdateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(id)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        if (dto.Email is not null && dto.Email != cliente.Email)
        {
            var otro = await clienteRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != id)
            {
                throw new ClienteDuplicadoException("Ya existe un cliente con ese email.");
            }

            cliente.Email = dto.Email;
        }

        if (dto.Nombre is not null) cliente.Nombre = dto.Nombre;
        if (dto.Telefono is not null) cliente.Telefono = dto.Telefono;

        await clienteRepository.UpdateAsync(cliente);
        await auditoriaService.RegistrarAsync("ActualizarClienteAdmin", ActorTipo.Admin, null, "Cliente", id);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Baja lógica (RN-14): preserva el historial de movimientos y canjes.</summary>
    public async Task DarDeBajaAsync(Guid id)
    {
        var cliente = await clienteRepository.GetByIdAsync(id)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        cliente.Activo = false;
        await clienteRepository.UpdateAsync(cliente);
        await auditoriaService.RegistrarAsync("DarDeBajaClienteAdmin", ActorTipo.Admin, null, "Cliente", id);
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
