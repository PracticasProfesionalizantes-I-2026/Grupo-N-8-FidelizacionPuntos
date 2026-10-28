using System.ComponentModel.DataAnnotations;
using FidelixAPI.Shared.Enums;

namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// El admin modifica un cliente (CU-15). Actualización parcial. A diferencia del perfil (CU-03),
/// el admin puede cambiar la categoría: así un cliente pasa a Gold (N1).
/// </summary>
public class AdminClienteUpdateDTO
{
    [MaxLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")]
    public string? Nombre { get; set; }

    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public CategoriaCliente? Categoria { get; set; }
}
