using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Canje;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de ICanjeService (CU-07). </summary>
public class CanjeService(
    IClienteRepository clienteRepository,
    IBeneficioRepository beneficioRepository,
    IMovimientoRepository movimientoRepository,
    ILotePuntosService lotePuntosService) : ICanjeService
{
    /// <summary> Estado inicial del canje: el beneficio todavía no se entregó (CU-11). </summary>
    private const string EstadoPendiente = "Pendiente";

    /// <summary>
    /// Canje: cliente existente, beneficio existente (3a) y activo (3b, RN-17), saldo
    /// suficiente (3c, RN-08) y descuento FIFO (RN-09). El descuento de los lotes y el
    /// movimiento de canje se guardan en un único `SaveChanges`, o sea, en una sola transacción.
    /// </summary>
    public async Task<CanjeResponseDTO> CrearCanjeAsync(Guid clienteId, CanjeCreateDTO dto)
    {
        _ = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("No se encontró el cliente.");

        var beneficio = await beneficioRepository.GetByIdAsync(dto.BeneficioId!.Value)
            ?? throw new BeneficioNotFoundException("No hay beneficios disponibles.");

        if (!beneficio.Activo)
        {
            throw new BeneficioInactivoException("El beneficio no pudo ser canjeado.");
        }

        var ahora = DateTime.UtcNow;
        IReadOnlyList<Movimiento> lotes;
        try
        {
            lotes = await movimientoRepository.GetLotesParaCanjeAsync(clienteId, ahora);
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
            ClienteId = clienteId,
            Tipo = TipoMovimiento.Canje,
            Puntos = -beneficio.CostoPuntos,
            PuntosDisponibles = 0,
            Fecha = ahora,
            BeneficioId = beneficio.Id,
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
            Estado = EstadoPendiente,
            Fecha = canje.Fecha
        };
    }
}
