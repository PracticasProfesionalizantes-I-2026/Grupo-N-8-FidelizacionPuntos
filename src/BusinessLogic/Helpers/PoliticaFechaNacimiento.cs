using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Helpers;

/// <summary>
/// Regla única de la fecha de nacimiento del cliente: no puede ser futura y la edad tiene que estar
/// entre <see cref="EdadMinima"/> y <see cref="EdadMaxima"/> años (el programa es para mayores de
/// edad y se descartan fechas imposibles como 1900). La usan todos los canales que crean o modifican
/// clientes (registro, perfil, punto de venta y ABM del admin), así la regla vive en un solo lugar.
/// </summary>
public static class PoliticaFechaNacimiento
{
    /// <summary>Edad mínima para participar del programa.</summary>
    public const int EdadMinima = 18;

    /// <summary>Edad máxima razonable: descarta fechas cargadas por error (ej. 1900).</summary>
    public const int EdadMaxima = 120;

    /// <summary>
    /// Lanza <see cref="FechaNacimientoInvalidaException"/> si la fecha está informada y no es válida.
    /// Una fecha nula no se valida: el dato es opcional.
    /// </summary>
    public static void Validar(DateTime? fechaNacimiento)
    {
        if (fechaNacimiento is not null && !EsValida(fechaNacimiento.Value, FechasNegocio.HoyArgentina()))
        {
            throw new FechaNacimientoInvalidaException(
                $"La fecha de nacimiento no es válida: el cliente debe tener entre {EdadMinima} y {EdadMaxima} años.");
        }
    }

    /// <summary>
    /// Indica si la fecha es válida respecto de <paramref name="hoy"/>: no futura y con una edad entre
    /// los límites. Recibe "hoy" como parámetro para poder probar casos borde con fechas fijas.
    /// </summary>
    public static bool EsValida(DateTime fechaNacimiento, DateOnly hoy)
    {
        var nacimiento = DateOnly.FromDateTime(fechaNacimiento);
        if (nacimiento > hoy)
        {
            return false;
        }

        var edad = CalcularEdad(nacimiento, hoy);
        return edad is >= EdadMinima and <= EdadMaxima;
    }

    /// <summary>
    /// Años cumplidos a la fecha <paramref name="hoy"/>: se resta uno si todavía no llegó el cumpleaños
    /// de este año (ej. nacido el 20/11/2008, el 19/11/2026 tiene 17 y el 20/11/2026 cumple 18). Un
    /// nacido el 29/2 cumple el 28/2 en los años no bisiestos (`AddYears` lo ajusta así; coincide con el
    /// art. 6 del Código Civil y Comercial).
    /// </summary>
    public static int CalcularEdad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        return nacimiento.AddYears(edad) > hoy ? edad - 1 : edad;
    }
}
