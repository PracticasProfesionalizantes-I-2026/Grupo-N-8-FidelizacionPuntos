using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IReporteService (CU-24). </summary>
public class ReporteService(IMovimientoRepository movimientoRepository, IAuditoriaRepository auditoriaRepository) : IReporteService
{
    /// <summary>
    /// Calcula los totales del período a partir de los movimientos (la misma fuente inmutable que se
    /// audita, RN-19) e informa cuántos registros de auditoría hay en el período para contrastar.
    /// Si el período no tiene movimientos, lo informa con un mensaje (flujo 4a).
    /// </summary>
    public async Task<ReporteResponseDTO> GenerarReporteAsync(ReporteQueryDTO query)
    {
        var desde = query.PeriodoDesde!.Value;
        // Si `periodoHasta` viene sin hora, se toma el día completo (hasta las 23:59:59).
        var hasta = query.PeriodoHasta!.Value.TimeOfDay == TimeSpan.Zero
            ? query.PeriodoHasta.Value.Date.AddDays(1).AddTicks(-1)
            : query.PeriodoHasta.Value;

        var movimientos = await movimientoRepository.BuscarAsync(null, null, desde, hasta);
        var canjes = movimientos.Where(m => m.Tipo == TipoMovimiento.Canje).ToList();

        var reporte = new ReporteResponseDTO
        {
            Tipo = query.Tipo!.Value.ToString(),
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            CantidadMovimientos = movimientos.Count,
            ClientesConMovimientos = movimientos.Select(m => m.ClienteId).Distinct().Count(),
            PuntosAcumulados = movimientos.Where(m => m.Tipo == TipoMovimiento.Acumulacion).Sum(m => m.Puntos),
            PuntosBonoCumpleanos = movimientos.Where(m => m.Tipo == TipoMovimiento.BonoCumpleanos).Sum(m => m.Puntos),
            PuntosCanjeados = -canjes.Sum(m => m.Puntos),
            PuntosVencidos = -movimientos.Where(m => m.Tipo == TipoMovimiento.Vencimiento).Sum(m => m.Puntos),
            RegistrosAuditoria = await auditoriaRepository.ContarAsync(desde, hasta),
            Mensaje = movimientos.Count == 0 ? "No hay datos disponibles para el período seleccionado" : null
        };

        if (query.Tipo == TipoReporte.BeneficiosMasCanjeados)
        {
            reporte.Ranking = canjes
                .Where(c => c.BeneficioId is not null)
                .GroupBy(c => c.BeneficioId!.Value)
                .Select(g => new RankingBeneficioDTO
                {
                    BeneficioId = g.Key,
                    Nombre = g.First().Beneficio?.Nombre ?? string.Empty,
                    CantidadCanjes = g.Count(),
                    PuntosCanjeados = -g.Sum(c => c.Puntos)
                })
                .OrderByDescending(r => r.CantidadCanjes)
                .ThenByDescending(r => r.PuntosCanjeados)
                .ToList();
        }

        return reporte;
    }
}
