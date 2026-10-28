using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de ICanjeService (CU-07, CU-11). </summary>
public class CanjeService(
    IClienteRepository clienteRepository,
    IBeneficioRepository beneficioRepository,
    IMovimientoRepository movimientoRepository,
    ILotePuntosService lotePuntosService) : ICanjeService
{
    /// <summary> CU-07: el cliente sale del token; el canje queda "Pendiente" hasta que retire el beneficio. </summary>
    public async Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto)
    {
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        return await EjecutarCanjeAsync(cliente, dto.BeneficioId!.Value, EstadoCanje.Pendiente, empleadoId: null);
    }

    /// <summary> CU-11: el cliente se busca por documento (1a); el beneficio se entrega en el momento. </summary>
    public async Task<CanjeResponseDTO> CrearCanjePresencialAsync(CanjePresencialCreateDTO dto, Guid? empleadoId)
    {
        var cliente = await clienteRepository.GetByDocumentoAsync(dto.ClienteDocumento)
            ?? throw new ClienteNotFoundException("El documento ingresado no se encuentra registrado");

        return await EjecutarCanjeAsync(cliente, dto.BeneficioId!.Value, EstadoCanje.Entregado, empleadoId);
    }

    /// <summary> RN-10: solo un canje existente y pendiente puede pasar a "Entregado". </summary>
    public async Task<CanjeItemDTO> MarcarEntregadoAsync(Guid canjeId, Guid? empleadoId)
    {
        var canje = await movimientoRepository.GetByIdAsync(canjeId);
        if (canje is null || canje.Tipo != TipoMovimiento.Canje)
        {
            throw new CanjeNotFoundException("El canje no existe.");
        }

        if (canje.EstadoCanje == EstadoCanje.Entregado)
        {
            throw new CanjeYaEntregadoException("El canje ya fue entregado.");
        }

        canje.EstadoCanje = EstadoCanje.Entregado;
        canje.EmpleadoId = empleadoId;
        await movimientoRepository.UpdateAsync(canje);
        return MovimientoMapper.ToCanjeItemDTO(canje);
    }

    /// <summary>
    /// Núcleo común de los dos canjes: cliente activo (RN-14), beneficio existente (3a) y activo
    /// (3b, RN-17), saldo suficiente (3c, RN-08) y descuento FIFO (RN-09). El descuento de los
    /// lotes y el movimiento de canje se guardan en un único `SaveChanges`, o sea, en una sola transacción.
    /// </summary>
    private async Task<CanjeResponseDTO> EjecutarCanjeAsync(Cliente cliente, Guid beneficioId, EstadoCanje estado, Guid? empleadoId)
    {
        if (!cliente.Activo)
        {
            throw new CuentaInactivaException("El cliente se encuentra inactivo.");
        }

        var beneficio = await beneficioRepository.GetByIdAsync(beneficioId)
            ?? throw new BeneficioNotFoundException("No hay beneficios disponibles.");

        if (!beneficio.Activo)
        {
            throw new BeneficioInactivoException("El beneficio no pudo ser canjeado.");
        }

        var ahora = DateTime.UtcNow;
        IReadOnlyList<Movimiento> lotes;
        try
        {
            lotes = await movimientoRepository.GetLotesParaCanjeAsync(cliente.Id, ahora);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo realizar el canje en este momento.", ex);
        }

        // RN-08: solo cuentan los puntos disponibles hoy (no los vencidos ni los pendientes de N2).
        var saldo = lotes.Sum(l => l.PuntosDisponibles);
        if (saldo < beneficio.CostoPuntos)
        {
            throw new SaldoInsuficienteException("Saldo insuficiente para hacer canje.");
        }

        // RN-09: descuenta de los lotes más antiguos primero (los lotes quedan modificados en memoria).
        lotePuntosService.AplicarFifo(lotes, beneficio.CostoPuntos);

        var canje = new Movimiento
        {
            ClienteId = cliente.Id,
            Tipo = TipoMovimiento.Canje,
            Puntos = -beneficio.CostoPuntos,
            PuntosDisponibles = 0,
            Fecha = ahora,
            BeneficioId = beneficio.Id,
            EstadoCanje = estado,
            EmpleadoId = empleadoId,
            Detalle = $"Canje: {beneficio.Nombre}"
        };

        try
        {
            // El SaveChanges de CreateAsync guarda el canje y también los lotes trackeados: todo o nada.
            canje = await movimientoRepository.CreateAsync(canje);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo realizar el canje en este momento.", ex);
        }

        return new CanjeResponseDTO
        {
            Id = canje.Id,
            BeneficioId = beneficio.Id,
            BeneficioNombre = beneficio.Nombre,
            PuntosDescontados = beneficio.CostoPuntos,
            SaldoRestante = saldo - beneficio.CostoPuntos,
            Estado = estado.ToString(),
            Fecha = canje.Fecha
        };
    }
}
