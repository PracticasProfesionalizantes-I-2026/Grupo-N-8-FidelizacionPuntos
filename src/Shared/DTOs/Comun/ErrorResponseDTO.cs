namespace FidelixAPI.Shared.DTOs.Comun;

/// <summary>
/// Cuerpo de una respuesta de error de negocio (400, 404, 409, 500): `{ "mensaje": "..." }`. Reemplaza a
/// los objetos anónimos para que todas las respuestas de la API sean DTOs con la misma forma y la misma
/// clave que <see cref="MensajeResponseDTO"/> y <c>ListadoResponseDTO</c>. Los 400 automáticos de
/// validación siguen siendo <c>ProblemDetails</c> (estándar de ASP.NET).
/// </summary>
public class ErrorResponseDTO
{
    /// <summary>Descripción del error, pensada para mostrarse al usuario.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Constructor vacío, necesario para deserializar la respuesta (por ejemplo, en los tests).</summary>
    public ErrorResponseDTO() { }

    /// <summary>Crea el error con el mensaje indicado (normalmente, el de la excepción de dominio).</summary>
    public ErrorResponseDTO(string mensaje) => Mensaje = mensaje;
}
