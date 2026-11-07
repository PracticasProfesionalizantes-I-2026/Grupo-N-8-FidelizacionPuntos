namespace FidelixAPI.Shared.DTOs.Beneficio;

/// <summary>
/// Una categoría de beneficio, para que el admin sepa qué número usar al crear o modificar un
/// beneficio (CU-17): `id` es el valor que se envía en `categoria` y `nombre` lo que significa.
/// </summary>
public class CategoriaBeneficioDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
