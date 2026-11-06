using FidelixAPI.BusinessLogic.Helpers;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Helpers;

/// <summary>
/// Tests de <see cref="FechasNegocio"/>: las fechas de los filtros son hora Argentina (UTC-3) y la
/// base guarda UTC. Usan fechas fijas para que el resultado no dependa del día en que se corren ni de
/// la zona horaria de la máquina (Linux, macOS o Windows).
/// </summary>
public class FechasNegocioTests
{
    /// <summary> La zona del negocio se resuelve en cualquier sistema operativo y es UTC-3 (sin horario de verano). </summary>
    [Fact]
    public void ZonaArgentina_IsUtcMinusThree()
    {
        Assert.Equal(TimeSpan.FromHours(-3), FechasNegocio.ZonaArgentina.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(TimeSpan.FromHours(-3), FechasNegocio.ZonaArgentina.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    /// <summary> Un día sin hora (desde = hasta) se consulta completo: de 00:00 a 23:59:59,9999999 hora Argentina, en UTC. </summary>
    [Fact]
    public void RangoUtc_WithDateOnlyRange_CoversWholeArgentineDay()
    {
        var dia = new DateTime(2026, 11, 6);

        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(dia, dia);

        Assert.Equal(new DateTime(2026, 11, 6, 3, 0, 0, DateTimeKind.Utc), desdeUtc);
        Assert.Equal(new DateTime(2026, 11, 7, 3, 0, 0, DateTimeKind.Utc).AddTicks(-1), hastaUtc);
        Assert.Equal(DateTimeKind.Utc, desdeUtc!.Value.Kind);
    }

    /// <summary>
    /// Caso pedido: una venta a las 22:30 hora Argentina del 06/11 se guarda como 07/11 01:30 UTC.
    /// Tiene que entrar en el filtro del día 06/11 y no en el del 07/11.
    /// </summary>
    [Fact]
    public void RangoUtc_WithSaleAfter21ArgentineTime_BelongsToSameArgentineDay()
    {
        var ventaUtc = new DateTime(2026, 11, 7, 1, 30, 0, DateTimeKind.Utc);

        var (desde6, hasta6) = FechasNegocio.RangoUtc(new DateTime(2026, 11, 6), new DateTime(2026, 11, 6));
        var (desde7, hasta7) = FechasNegocio.RangoUtc(new DateTime(2026, 11, 7), new DateTime(2026, 11, 7));

        Assert.InRange(ventaUtc, desde6!.Value, hasta6!.Value);
        Assert.False(ventaUtc >= desde7 && ventaUtc <= hasta7);
        Assert.Equal(new DateTime(2026, 11, 6, 22, 30, 0), FechasNegocio.AHoraArgentina(ventaUtc));
    }

    /// <summary> Si el filtro trae hora, se respeta (solo se convierte de hora Argentina a UTC, sin extender al fin del día). </summary>
    [Fact]
    public void RangoUtc_WithExplicitTime_OnlyConvertsToUtc()
    {
        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(new DateTime(2026, 11, 6, 10, 0, 0), new DateTime(2026, 11, 6, 18, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 6, 13, 0, 0), desdeUtc);
        Assert.Equal(new DateTime(2026, 11, 6, 21, 30, 0), hastaUtc);
    }

    /// <summary> Una fecha que ya viene en UTC (con `Z`) no se vuelve a convertir ni se extiende. </summary>
    [Fact]
    public void RangoUtc_WithUtcValues_KeepsThemUnchanged()
    {
        var desde = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc);

        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(desde, hasta);

        Assert.Equal(desde, desdeUtc);
        Assert.Equal(hasta, hastaUtc);
    }

    /// <summary> Un filtro no informado sigue siendo nulo (no se inventan fechas). </summary>
    [Fact]
    public void RangoUtc_WithNullValues_ReturnsNulls()
    {
        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(null, null);

        Assert.Null(desdeUtc);
        Assert.Null(hastaUtc);
    }

    /// <summary> Las fechas leídas de SQLite vienen sin Kind: se tratan como UTC al pasarlas a hora Argentina. </summary>
    [Fact]
    public void AHoraArgentina_WithUnspecifiedKind_TreatsValueAsUtc()
    {
        var deLaBase = new DateTime(2026, 11, 6, 21, 33, 0, DateTimeKind.Unspecified);

        Assert.Equal(new DateTime(2026, 11, 6, 18, 33, 0), FechasNegocio.AHoraArgentina(deLaBase));
    }
}
