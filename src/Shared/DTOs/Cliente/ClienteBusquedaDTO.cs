using System.ComponentModel.DataAnnotations;

namespace FidelixAPI.Shared.DTOs.Cliente;

/// <summary>
/// Criterio de búsqueda de un cliente por DNI desde el punto de venta (CU-10, CU-11, CU-13), recibido
/// como query param. Es el único lugar donde el DNI identifica a un cliente: con el id que devuelve la
/// búsqueda, el resto de las operaciones trabajan por Guid. El formato (7 a 10 dígitos) lo valida la
/// Capa de Negocio y responde 400.
/// </summary>
public class ClienteBusquedaDTO
{
    [Required(ErrorMessage = "El documento es obligatorio.")]
    public string Documento { get; set; } = string.Empty;
}
