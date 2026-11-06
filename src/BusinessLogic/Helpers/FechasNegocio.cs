namespace FidelixAPI.BusinessLogic.Helpers;

/// <summary>
/// Regla única de fechas del negocio: las fechas que escribe un usuario en un filtro (CU-05, CU-20,
/// CU-24) están en <b>hora Argentina</b>, pero los movimientos y la auditoría se guardan en <b>UTC</b>.
/// Esta clase hace la traducción en un solo lugar, para que todas las consultas cuenten los mismos
/// días (si cada service lo resolviera a su manera, el reporte y la consulta de movimientos podían
/// dar resultados distintos para el mismo período).
/// </summary>
public static class FechasNegocio
{
    /// <summary>
    /// Zona horaria del negocio. Se resuelve una sola vez al iniciar; ver <see cref="ResolverZonaArgentina"/>.
    /// </summary>
    public static TimeZoneInfo ZonaArgentina { get; } = ResolverZonaArgentina();

    /// <summary>
    /// Traduce un rango de filtro (hora Argentina) al rango UTC con el que se consulta la base.
    /// Si `hasta` viene sin hora (ej. `2026-11-06`), incluye ese día completo hasta las 23:59:59,9999999.
    /// Los valores nulos siguen nulos (filtro no informado).
    /// </summary>
    public static (DateTime? DesdeUtc, DateTime? HastaUtc) RangoUtc(DateTime? desde, DateTime? hasta) =>
        (desde is null ? null : AUtc(desde.Value),
         hasta is null ? null : AUtc(FinDelDiaSiNoTieneHora(hasta.Value)));

    /// <summary>
    /// Si la fecha "hasta" no trae hora, la lleva al último instante de ese día; si trae hora, la deja
    /// igual. Una fecha que ya viene en UTC (con `Z`) se respeta tal cual: el usuario fue explícito.
    /// </summary>
    public static DateTime FinDelDiaSiNoTieneHora(DateTime hasta) =>
        hasta.Kind != DateTimeKind.Utc && hasta.TimeOfDay == TimeSpan.Zero
            ? hasta.Date.AddDays(1).AddTicks(-1)
            : hasta;

    /// <summary>
    /// Convierte una fecha ingresada por el usuario a UTC:
    /// - `Utc` (vino con `Z`): ya está en UTC, no se toca.
    /// - `Local` (vino con un offset, ej. `-03:00`): .NET ya sabe a qué instante corresponde.
    /// - `Unspecified` (vino sin zona, el caso normal): se interpreta como hora Argentina.
    /// </summary>
    public static DateTime AUtc(DateTime fecha) => fecha.Kind switch
    {
        DateTimeKind.Utc => fecha,
        DateTimeKind.Local => fecha.ToUniversalTime(),
        _ => TimeZoneInfo.ConvertTimeToUtc(fecha, ZonaArgentina)
    };

    /// <summary>
    /// Convierte una fecha guardada en la base (UTC) a hora Argentina, por ejemplo para agrupar una
    /// serie por día. SQLite devuelve las fechas sin `Kind`, por eso se marcan como UTC antes de convertir.
    /// </summary>
    public static DateTime AHoraArgentina(DateTime fechaUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc), ZonaArgentina);

    /// <summary>
    /// Busca la zona de Argentina de forma que funcione en Linux, macOS y Windows:
    /// 1. Id IANA (Linux/macOS, y Windows con .NET 6+ cuando tiene ICU).
    /// 2. Id de Windows (Windows en modo NLS, sin ICU).
    /// 3. Si el sistema no tiene la base de zonas (ej. contenedor mínimo), una zona fija UTC-3:
    ///    es correcta porque Argentina no usa horario de verano desde 2009.
    /// </summary>
    private static TimeZoneInfo ResolverZonaArgentina()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Se prueba con el siguiente id.
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Argentina", TimeSpan.FromHours(-3), "Argentina (UTC-3)", "Argentina (UTC-3)");
    }
}
