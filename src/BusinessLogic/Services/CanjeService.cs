using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="ICanjeService"/> (CU-07, CU-08, CU-11, CU-13, CU-23).</summary>
public class CanjeService(
    IMovimientoRepository movimientoRepository,
    IBeneficioRepository beneficioRepository,
    IClienteRepository clienteRepository,
    IPuntosService puntosService,
    IAuditoriaService auditoriaService) : ICanjeService
{
    /// <summary>El propio cliente canjea (CU-07): valida beneficio y descuenta FIFO (RN-09) vía <see cref="IPuntosService"/>.</summary>
    public async Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        if (!cliente.Activo)
        {
            throw new CuentaInactivaException("El cliente está dado de baja y no puede canjear puntos."); // RN-14
        }

        return await EjecutarCanjeAsync(cliente, dto.BeneficioId, empleadoId: null);
    }

    /// <summary>Un empleado canjea de forma presencial para un cliente identificado por documento (CU-11).</summary>
    public async Task<CanjeResponseDTO> CrearCanjePresencialAsync(Guid empleadoId, CanjeCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClienteDocumento))
        {
            throw new ValidationException("Debe indicarse el documento del cliente.");
        }

        var cliente = await clienteRepository.GetByDocumentoAsync(dto.ClienteDocumento)
            ?? throw new ClienteNotFoundException("El documento ingresado no se encuentra registrado.");

        if (!cliente.Activo)
        {
            throw new CuentaInactivaException("El cliente está dado de baja y no puede canjear puntos."); // RN-14
        }

        return await EjecutarCanjeAsync(cliente, dto.BeneficioId, empleadoId);
    }

    /// <summary>Lógica común a CU-07/CU-11: valida el beneficio, descuenta puntos y registra el movimiento.</summary>
    private async Task<CanjeResponseDTO> EjecutarCanjeAsync(Cliente cliente, Guid beneficioId, Guid? empleadoId)
    {
        var beneficio = await beneficioRepository.GetByIdAsync(beneficioId)
            ?? throw new BeneficioNotFoundException("No se encontró el beneficio.");

        if (!beneficio.Activo)
        {
            throw new BeneficioInactivoException("El beneficio no está disponible para canjear."); // RN-17
        }

        await puntosService.DescontarPuntosFifoAsync(cliente.Id, beneficio.CostoPuntos); // RN-08, RN-09

        Movimiento movimiento;
        try
        {
            movimiento = await movimientoRepository.CreateAsync(new Movimiento
            {
                ClienteId = cliente.Id,
                Tipo = TipoMovimiento.Canje,
                Puntos = -beneficio.CostoPuntos,
                PuntosDisponibles = 0, // No aplica a un movimiento de salida: no es un lote propio.
                BeneficioId = beneficio.Id,
                EmpleadoId = empleadoId,
                Detalle = $"Canje de '{beneficio.Nombre}'"
            });
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo registrar el canje.", ex);
        }

        var actorTipo = empleadoId is null ? ActorTipo.Cliente : ActorTipo.Empleado;
        await auditoriaService.RegistrarAsync("CrearCanje", actorTipo, empleadoId ?? cliente.Id, "Cliente", cliente.Id, $"Canje de '{beneficio.Nombre}' por {beneficio.CostoPuntos} puntos");

        return MapToResponseDTO(movimiento);
    }

    /// <summary>Canjes propios del cliente autenticado (CU-08).</summary>
    public async Task<IReadOnlyList<CanjeResponseDTO>> ConsultarPorClienteIdAsync(Guid clienteId)
    {
        var movimientos = await movimientoRepository.GetByClienteIdAsync(clienteId);
        return movimientos.Where(m => m.Tipo == TipoMovimiento.Canje).Select(MapToResponseDTO).ToList();
    }

    /// <summary>Canjes de un cliente puntual, consultados por un empleado (CU-13).</summary>
    public async Task<IReadOnlyList<CanjeResponseDTO>> ConsultarPorDocumentoAsync(string documento)
    {
        var cliente = await clienteRepository.GetByDocumentoAsync(documento)
            ?? throw new ClienteNotFoundException("El documento ingresado no se encuentra registrado.");

        return await ConsultarPorClienteIdAsync(cliente.Id);
    }

    /// <summary>Traduce el movimiento de tipo Canje al DTO público.</summary>
    private static CanjeResponseDTO MapToResponseDTO(Movimiento m) => new()
    {
        Id = m.Id,
        ClienteId = m.ClienteId,
        BeneficioId = m.BeneficioId!.Value,
        PuntosUtilizados = -m.Puntos,
        Fecha = m.Fecha,
        EmpleadoId = m.EmpleadoId
    };
}
