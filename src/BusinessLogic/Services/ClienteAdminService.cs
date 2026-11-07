using FidelixAPI.BusinessLogic.Helpers;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IClienteAdminService (CU-15). </summary>
public class ClienteAdminService(IClienteRepository clienteRepository) : IClienteAdminService
{
    /// <summary> Lista todos los clientes con la forma común de los listados `{ mensaje, items }`; vacío no es error. </summary>
    public async Task<ListadoResponseDTO<ClienteResponseDTO>> ListarClientesAsync()
    {
        var clientes = await clienteRepository.GetAllAsync();
        return ListadoResponseDTO<ClienteResponseDTO>.Crear(clientes.Select(MapToResponseDTO).ToList(), "No hay clientes registrados");
    }

    /// <summary> Alta: documento/email libres (RN-01, flujo 3a); si trae contraseña, tiene que cumplir RN-02. </summary>
    public async Task<ClienteResponseDTO> CrearClienteAsync(ClienteCreateDTO dto)
    {
        if (dto.Password is not null && !PoliticaPassword.EsValida(dto.Password))
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        // Fecha de nacimiento: no futura y con una edad entre 18 y 120 años.
        PoliticaFechaNacimiento.Validar(dto.FechaNacimiento);

        if (await clienteRepository.GetByDocumentoAsync(dto.Documento) is not null)
        {
            throw new ClienteDuplicadoException("El documento ingresado pertenece a un cliente activo");
        }

        if (await clienteRepository.GetByEmailAsync(dto.Email) is not null)
        {
            throw new ClienteDuplicadoException("Ya existe un cliente registrado con ese email.");
        }

        var cliente = await clienteRepository.CreateAsync(new Cliente
        {
            Nombre = dto.Nombre,
            Documento = dto.Documento,
            Email = dto.Email,
            PasswordHash = dto.Password is null ? null : PasswordHasher.Hash(dto.Password),
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        });

        return MapToResponseDTO(cliente);
    }

    /// <summary> Modificación parcial: 404 si no existe (3b) y email único si cambia (RN-01). </summary>
    public async Task<ClienteResponseDTO> ActualizarClienteAsync(int id, AdminClienteUpdateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(id)
            ?? throw new ClienteNotFoundException("El cliente no existe.");

        // Fecha de nacimiento: no futura y con una edad entre 18 y 120 años.
        PoliticaFechaNacimiento.Validar(dto.FechaNacimiento);

        if (dto.Email is not null && dto.Email != cliente.Email)
        {
            var otro = await clienteRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != id)
            {
                throw new ClienteDuplicadoException("Ya existe un cliente registrado con ese email.");
            }

            cliente.Email = dto.Email;
        }

        if (dto.Nombre is not null) cliente.Nombre = dto.Nombre;
        if (dto.Telefono is not null) cliente.Telefono = dto.Telefono;
        if (dto.FechaNacimiento is not null) cliente.FechaNacimiento = dto.FechaNacimiento;
        if (dto.Categoria is not null) cliente.Categoria = dto.Categoria.Value;

        await clienteRepository.UpdateAsync(cliente);
        return MapToResponseDTO(cliente);
    }

    /// <summary> Baja lógica (RN-14): conserva el historial de movimientos. </summary>
    public async Task BajaClienteAsync(int id)
    {
        var cliente = await clienteRepository.GetByIdAsync(id)
            ?? throw new ClienteNotFoundException("El cliente no existe.");

        cliente.Activo = false;
        await clienteRepository.UpdateAsync(cliente);
    }

    /// <summary> Traduce la entidad al DTO público, sin el hash de la contraseña. </summary>
    private static ClienteResponseDTO MapToResponseDTO(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Documento = c.Documento,
        Email = c.Email,
        Telefono = c.Telefono,
        FechaNacimiento = c.FechaNacimiento,
        Activo = c.Activo,
        FechaRegistro = c.FechaRegistro,
        Categoria = c.Categoria,
        Roles = [Roles.Cliente]
    };
}
