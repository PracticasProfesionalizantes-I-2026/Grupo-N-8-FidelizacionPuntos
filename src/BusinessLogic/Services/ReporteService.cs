using FidelixAPI.BusinessLogic.Helpers;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Reporte;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IReporteService (CU-24). </summary>
public class ReporteService(IMovimientoRepository movimientoRepository, IAuditoriaRepository auditoriaRepository) : IReporteService
{
    /// <summary>Mensaje del flujo 4a: el período pedido no tiene movimientos.</summary>
    private const string MensajeSinDatos = "No hay datos disponibles para el período seleccionado";

    /// <summary>
    /// Calcula los totales del período a partir de los movimientos (la misma fuente inmutable que se
    /// audita, RN-19) e informa cuántos registros de auditoría (de cualquier operación) hay en el
    /// período. Si el período no tiene movimientos, lo informa con un mensaje (flujo 4a).
    /// </summary>
    public async Task<ReporteResponseDTO> GenerarReporteAsync(ReporteQueryDTO query)
    {
        // El período se informa en hora Argentina; si `periodoHasta` viene sin hora, es el día completo.
        var desde = query.PeriodoDesde!.Value;
        var hasta = FechasNegocio.FinDelDiaSiNoTieneHora(query.PeriodoHasta!.Value);
        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(desde, hasta);

        var movimientos = await movimientoRepository.BuscarAsync(null, null, desdeUtc, hastaUtc);
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
            RegistrosAuditoria = await auditoriaRepository.ContarAsync(desdeUtc!.Value, hastaUtc!.Value),
            Mensaje = movimientos.Count == 0 ? MensajeSinDatos : null
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

    /// <summary>
    /// Comparación del dashboard ("Tipo 1 vs Tipo 2" del prototipo):
    /// - sin indicador: una serie por cada indicador en el período 1 (misma fecha, distinto movimiento);
    /// - con indicador: una serie del período 1 y, si se pidió, otra del período 2 (fechas distintas).
    /// Los movimientos se traen una sola vez por período y se agrupan por día en hora Argentina.
    /// </summary>
    public async Task<ComparacionResponseDTO> CompararAsync(ComparacionQueryDTO query)
    {
        var periodos = new List<(DateTime Desde, DateTime Hasta)> { (query.Periodo1Desde!.Value, query.Periodo1Hasta!.Value) };
        if (query.TieneSegundoPeriodo())
        {
            periodos.Add((query.Periodo2Desde!.Value, query.Periodo2Hasta!.Value));
        }

        IndicadorDashboard[] indicadores = query.Indicador is { } indicador ? [indicador] : Enum.GetValues<IndicadorDashboard>();
        var respuesta = new ComparacionResponseDTO();

        foreach (var (desde, hasta) in periodos)
        {
            var hastaDiaCompleto = FechasNegocio.FinDelDiaSiNoTieneHora(hasta);
            var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(desde, hastaDiaCompleto);
            var movimientos = await movimientoRepository.BuscarAsync(null, null, desdeUtc, hastaUtc);

            respuesta.Series.AddRange(indicadores.Select(i => ArmarSerie(i, desde, hastaDiaCompleto, movimientos)));
        }

        respuesta.Mensaje = respuesta.Series.All(s => s.Total == 0) ? MensajeSinDatos : null;
        return respuesta;
    }

    /// <summary>
    /// Arma la serie diaria de un indicador: un punto por cada día del período (en 0 si ese día no hubo
    /// movimientos), agrupando cada movimiento por su fecha en hora Argentina.
    /// </summary>
    private static SerieComparacionDTO ArmarSerie(IndicadorDashboard indicador, DateTime desde, DateTime hasta, IReadOnlyList<Movimiento> movimientos)
    {
        var porDia = movimientos
            .GroupBy(m => DateOnly.FromDateTime(FechasNegocio.AHoraArgentina(m.Fecha)))
            .ToDictionary(g => g.Key, g => Medir(indicador, g));

        var primerDia = DateOnly.FromDateTime(desde);
        var ultimoDia = DateOnly.FromDateTime(hasta);
        var valores = new List<PuntoSerieDTO>();
        for (var dia = primerDia; dia <= ultimoDia; dia = dia.AddDays(1))
        {
            valores.Add(new PuntoSerieDTO { Fecha = dia, Valor = porDia.GetValueOrDefault(dia) });
        }

        return new SerieComparacionDTO
        {
            Indicador = indicador.ToString(),
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            Total = valores.Sum(v => v.Valor),
            Valores = valores
        };
    }

    /// <summary>
    /// Valor de un indicador sobre un grupo de movimientos: Ventas = cantidad de compras (acumulaciones),
    /// Canjes = cantidad de canjes, PuntosOtorgados = puntos de acumulaciones y bonos de cumpleaños.
    /// </summary>
    private static int Medir(IndicadorDashboard indicador, IEnumerable<Movimiento> movimientos) => indicador switch
    {
        IndicadorDashboard.Ventas => movimientos.Count(m => m.Tipo == TipoMovimiento.Acumulacion),
        IndicadorDashboard.Canjes => movimientos.Count(m => m.Tipo == TipoMovimiento.Canje),
        IndicadorDashboard.PuntosOtorgados => movimientos
            .Where(m => m.Tipo is TipoMovimiento.Acumulacion or TipoMovimiento.BonoCumpleanos)
            .Sum(m => m.Puntos),
        _ => throw new ArgumentOutOfRangeException(nameof(indicador), indicador, "Indicador no soportado.")
    };
}
