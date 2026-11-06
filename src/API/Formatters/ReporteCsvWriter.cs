using System.Globalization;
using System.Text;
using FidelixAPI.Shared.DTOs.Reporte;

namespace FidelixAPI.API.Formatters;

/// <summary>
/// Convierte un reporte (CU-24) a CSV para descargarlo. Vive en la capa API porque es una cuestión
/// de presentación: el service arma el mismo <see cref="ReporteResponseDTO"/> para la pantalla y para
/// la descarga, y acá solo cambia la forma de escribirlo, así el contenido es idéntico (sub-variación 1).
/// </summary>
public static class ReporteCsvWriter
{
    /// <summary>Formato de fecha del CSV: sin ambigüedad día/mes y ordenable.</summary>
    private const string FormatoFecha = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Escribe el reporte como CSV (separador coma, RFC 4180) en UTF-8 con BOM, para que una planilla
    /// de cálculo muestre bien los acentos. Primero van los totales (Campo,Valor) y, si el reporte es
    /// BeneficiosMasCanjeados, después una línea en blanco y la tabla del ranking.
    /// </summary>
    public static byte[] Escribir(ReporteResponseDTO reporte)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Campo,Valor");
        Fila(csv, "Tipo", reporte.Tipo);
        Fila(csv, "PeriodoDesde", reporte.PeriodoDesde.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        Fila(csv, "PeriodoHasta", reporte.PeriodoHasta.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        Fila(csv, "Mensaje", reporte.Mensaje ?? string.Empty);
        Fila(csv, "CantidadMovimientos", Numero(reporte.CantidadMovimientos));
        Fila(csv, "ClientesConMovimientos", Numero(reporte.ClientesConMovimientos));
        Fila(csv, "PuntosAcumulados", Numero(reporte.PuntosAcumulados));
        Fila(csv, "PuntosBonoCumpleanos", Numero(reporte.PuntosBonoCumpleanos));
        Fila(csv, "PuntosCanjeados", Numero(reporte.PuntosCanjeados));
        Fila(csv, "PuntosVencidos", Numero(reporte.PuntosVencidos));
        Fila(csv, "RegistrosAuditoria", Numero(reporte.RegistrosAuditoria));

        if (reporte.Ranking.Count > 0)
        {
            csv.AppendLine();
            csv.AppendLine("BeneficioId,Nombre,CantidadCanjes,PuntosCanjeados");
            foreach (var fila in reporte.Ranking)
            {
                csv.AppendLine(string.Join(',', fila.BeneficioId.ToString(), Escapar(fila.Nombre), Numero(fila.CantidadCanjes), Numero(fila.PuntosCanjeados)));
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    /// <summary>Agrega una fila "campo,valor" escapando el valor.</summary>
    private static void Fila(StringBuilder csv, string campo, string valor) => csv.AppendLine($"{campo},{Escapar(valor)}");

    /// <summary>Escribe un número sin separador de miles, igual en cualquier configuración regional.</summary>
    private static string Numero(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Escapa un valor según RFC 4180: si tiene coma, comillas o salto de línea, va entre comillas
    /// y las comillas internas se duplican (ej. el nombre de un beneficio "Café, grande").
    /// </summary>
    private static string Escapar(string valor) =>
        valor.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
}
