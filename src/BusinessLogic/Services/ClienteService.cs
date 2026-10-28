using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.DTOs.Cliente;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IClienteService"/> (CU-01, CU-03, CU-04, CU-05, CU-08, CU-12, CU-13).</summary>
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

    /// <summary>Perfil del cliente autenticado (CU-03).</summary>
    public async Task<ClienteResponseDTO> ObtenerPerfilAsync(Guid clienteId)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");
        return MapToResponseDTO(cliente);
    }

    /// <summary>
    /// Modificación parcial del perfil (CU-03): rechaza cambiar el documento (2b, RN-04), valida la
    /// contraseña nueva (RN-02) y que el email nuevo no sea de otro cliente (3a, RN-01).
    /// </summary>
    public async Task<ClienteResponseDTO> ActualizarPerfilAsync(Guid clienteId, ClienteUpdateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        if (dto.Documento is not null && dto.Documento != cliente.Documento)
        {
            throw new DocumentoNoModificableException("El documento de identidad no puede ser modificado");
        }

        if (dto.Password is not null && (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8))
        {
            throw new PasswordInvalidaException("La contraseña no cumple los requisitos mínimos.");
        }

        if (dto.Email is not null && dto.Email != cliente.Email)
        {
            var otro = await clienteRepository.GetByEmailAsync(dto.Email);
            if (otro is not null && otro.Id != clienteId)
            {
                throw new EmailDuplicadoException("El email ingresado ya se encuentra registrado");
            }

            cliente.Email = dto.Email;
        }

        if (dto.Nombre is not null) cliente.Nombre = dto.Nombre;
        if (dto.Telefono is not null) cliente.Telefono = dto.Telefono;
        if (dto.FechaNacimiento is not null) cliente.FechaNacimiento = dto.FechaNacimiento;
        if (dto.Password is not null) cliente.PasswordHash = PasswordHasher.Hash(dto.Password);

        await clienteRepository.UpdateAsync(cliente);
        return MapToResponseDTO(cliente);
    }

    /// <summary>Baja voluntaria (CU-03): `Activo = false`, el historial queda intacto (RN-14).</summary>
    public async Task DarseDeBajaAsync(Guid clienteId)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        cliente.Activo = false;
        await clienteRepository.UpdateAsync(cliente);
    }

    /// <summary>Historial con filtros, en orden cronológico (CU-05, RN-06); vacío no es error (4a).</summary>
    public async Task<ListadoResponseDTO<MovimientoResponseDTO>> ConsultarHistorialAsync(Guid clienteId, HistorialFiltroDTO filtro)
    {
        var movimientos = await movimientoRepository.BuscarAsync(clienteId, filtro.TipoMovimiento, filtro.FechaDesde, filtro.FechaHasta);
        var items = movimientos.OrderBy(m => m.Fecha).Select(MovimientoMapper.ToResponseDTO).ToList();
        return ListadoResponseDTO<MovimientoResponseDTO>.Crear(items, "No hay movimientos para mostrar");
    }

    /// <summary>Canjes del cliente autenticado con su estado (CU-08, RN-10); vacío no es error (4a).</summary>
    public async Task<ListadoResponseDTO<CanjeItemDTO>> ConsultarCanjesAsync(Guid clienteId)
    {
        var canjes = await movimientoRepository.BuscarAsync(clienteId, TipoMovimiento.Canje, null, null);
        return ListadoResponseDTO<CanjeItemDTO>.Crear(canjes.Select(MovimientoMapper.ToCanjeItemDTO).ToList(), "Aún no realizaste ningún canje");
    }

    /// <summary>
    /// Alta desde el punto de venta (CU-12): no lleva contraseña (el cliente la define después con
    /// la recuperación, CU-25) y exige documento/email libres (3a, RN-01).
    /// </summary>
    public async Task<ClienteResponseDTO> CrearClienteDesdePOSAsync(ClienteCreateDTO dto)
    {
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
            PasswordHash = null,
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        });

        return MapToResponseDTO(cliente);
    }

    /// <summary>Canjes de un cliente por documento (CU-13): 404 si no existe (3a); vacío no es error (4a).</summary>
    public async Task<ListadoResponseDTO<CanjeItemDTO>> ConsultarCanjesPorDocumentoAsync(string documento)
    {
        var cliente = await clienteRepository.GetByDocumentoAsync(documento)
            ?? throw new ClienteNotFoundException("El DNI ingresado es inválido");

        var canjes = await movimientoRepository.BuscarAsync(cliente.Id, TipoMovimiento.Canje, null, null);
        return ListadoResponseDTO<CanjeItemDTO>.Crear(canjes.Select(MovimientoMapper.ToCanjeItemDTO).ToList(), "El cliente aún no registra beneficios canjeados");
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
        FechaRegistro = c.FechaRegistro,
        Categoria = c.Categoria
    };
}
