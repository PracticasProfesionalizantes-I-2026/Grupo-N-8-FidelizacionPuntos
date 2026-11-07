using FidelixAPI.BusinessLogic.Helpers;
using FidelixAPI.Shared.Exceptions;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Helpers;

/// <summary>
/// Tests de <see cref="PoliticaFechaNacimiento"/>: no futura y edad entre 18 y 120 años. Usan un "hoy"
/// fijo (20/11/2026) para que los casos borde no dependan del día en que se corren.
/// </summary>
public class PoliticaFechaNacimientoTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 20);

    /// <summary> Caso borde: quien cumple 18 hoy ya es válido. </summary>
    [Fact]
    public void EsValida_WhenTurns18Today_ReturnsTrue() =>
        Assert.True(PoliticaFechaNacimiento.EsValida(new DateTime(2008, 11, 20), Hoy));

    /// <summary> Caso borde: quien cumple 18 mañana todavía tiene 17. </summary>
    [Fact]
    public void EsValida_WhenTurns18Tomorrow_ReturnsFalse() =>
        Assert.False(PoliticaFechaNacimiento.EsValida(new DateTime(2008, 11, 21), Hoy));

    /// <summary> Una fecha futura (el caso de 2100 encontrado en Bruno) se rechaza. </summary>
    [Fact]
    public void EsValida_WithFutureDate_ReturnsFalse() =>
        Assert.False(PoliticaFechaNacimiento.EsValida(new DateTime(2100, 1, 1), Hoy));

    /// <summary> Una fecha absurda (el caso de 1900 encontrado en Bruno: 126 años) se rechaza. </summary>
    [Fact]
    public void EsValida_With1900_ReturnsFalse() =>
        Assert.False(PoliticaFechaNacimiento.EsValida(new DateTime(1900, 1, 1), Hoy));

    /// <summary> Caso borde: 120 años es el máximo aceptado y 121 ya no. </summary>
    [Fact]
    public void EsValida_At120And121Years_AcceptsOnly120()
    {
        Assert.True(PoliticaFechaNacimiento.EsValida(new DateTime(1906, 11, 20), Hoy));
        Assert.False(PoliticaFechaNacimiento.EsValida(new DateTime(1905, 11, 20), Hoy));
    }

    /// <summary>
    /// Nacido un 29 de febrero: en un año no bisiesto cumple años el 28 de febrero. Coincide con el
    /// Código Civil y Comercial (art. 6: si el mes no tiene ese día, el plazo vence el último día del
    /// mes) y con `DateOnly.AddYears`, que lleva el 29/2 al 28/2.
    /// </summary>
    [Fact]
    public void CalcularEdad_WithLeapDayBirth_CountsBirthdayOnFebruary28()
    {
        var nacimiento = new DateOnly(2008, 2, 29);

        Assert.Equal(17, PoliticaFechaNacimiento.CalcularEdad(nacimiento, new DateOnly(2026, 2, 27)));
        Assert.Equal(18, PoliticaFechaNacimiento.CalcularEdad(nacimiento, new DateOnly(2026, 2, 28)));
    }

    /// <summary> Sin fecha no hay nada que validar: el dato es opcional. </summary>
    [Fact]
    public void Validar_WithNull_DoesNotThrow() => PoliticaFechaNacimiento.Validar(null);

    /// <summary> Con una fecha inválida, Validar lanza la excepción que el controller traduce a 400. </summary>
    [Fact]
    public void Validar_WithInvalidDate_ThrowsFechaNacimientoInvalidaException() =>
        Assert.Throws<FechaNacimientoInvalidaException>(() => PoliticaFechaNacimiento.Validar(new DateTime(2100, 1, 1)));
}
