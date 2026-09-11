using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary>Implementación de <see cref="IReporteService"/> (CU-24).</summary>
public class ReporteService(IMovimientoRepository movimientoRepository, IClienteRepository clienteRepository) : IReporteService
{
    /// <summary>Agrega los datos de origen (Movimientos/Clientes) según el tipo pedido; RN-19: mismos datos que auditoría.</summary>
    public async Task<ReporteResponseDTO> GenerarReporteAsync(TipoReporte tipo, DateTime periodoDesde, DateTime periodoHasta)
    {
        if (periodoDesde > periodoHasta)
        {
            throw new ValidationException("El período indicado es inválido: 'periodoDesde' es posterior a 'periodoHasta'.");
        }

        var reporte = tipo switch
        {
            TipoReporte.Acumulaciones => await GenerarReporteDeMovimientosAsync(TipoMovimiento.Acumulacion, periodoDesde, periodoHasta),
            TipoReporte.Canjes => await GenerarReporteDeMovimientosAsync(TipoMovimiento.Canje, periodoDesde, periodoHasta),
            TipoReporte.ClientesActivos => await GenerarReporteDeClientesActivosAsync(periodoDesde, periodoHasta),
            _ => throw new ValidationException("El tipo de reporte solicitado no existe.")
        };

        if (reporte.TotalRegistros == 0)
        {
            reporte.Mensaje = "No hay datos disponibles para el período seleccionado."; // 4a: no es un error.
        }

        return reporte;
    }

    private async Task<ReporteResponseDTO> GenerarReporteDeMovimientosAsync(TipoMovimiento tipoMovimiento, DateTime desde, DateTime hasta)
    {
        var movimientos = await movimientoRepository.ConsultarAsync(clienteId: null, tipoMovimiento, desde, hasta);
        return new ReporteResponseDTO
        {
            Tipo = tipoMovimiento == TipoMovimiento.Acumulacion ? TipoReporte.Acumulaciones : TipoReporte.Canjes,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            TotalRegistros = movimientos.Count,
            TotalPuntos = movimientos.Sum(m => Math.Abs(m.Puntos))
        };
    }

    private async Task<ReporteResponseDTO> GenerarReporteDeClientesActivosAsync(DateTime desde, DateTime hasta)
    {
        var clientes = await clienteRepository.GetAllAsync();
        var activosEnPeriodo = clientes.Count(c => c.Activo && c.FechaRegistro >= desde && c.FechaRegistro <= hasta);

        return new ReporteResponseDTO
        {
            Tipo = TipoReporte.ClientesActivos,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            TotalRegistros = activosEnPeriodo,
            TotalPuntos = null
        };
    }
}
