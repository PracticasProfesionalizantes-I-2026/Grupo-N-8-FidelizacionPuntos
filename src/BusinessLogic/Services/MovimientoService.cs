using System.Text.RegularExpressions;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IMovimientoService (CU-10). </summary>
public class MovimientoService(
    IClienteRepository clienteRepository,
    IProductoRepository productoRepository,
    IReglaAcumulacionRepository reglaRepository,
    IMovimientoRepository movimientoRepository) : IMovimientoService
{
    /// <summary> N1: multiplicador de los clientes Gold. </summary>
    private const decimal MultiplicadorGold = 1.5m;
    /// <summary> N2: días que los puntos quedan pendientes antes de poder canjearse. </summary>
    private const int DiasPendiente = 15;
    /// <summary> Flujo 3b: el documento son solo números, de 7 a 10 dígitos. </summary>
    private static readonly Regex FormatoDocumento = new(@"^\d{7,10}$");

    /// <summary>
    /// Registra la compra: valida el documento (3b), busca al cliente (3a), calcula el monto con
    /// los precios de los productos (2a, RN-26), aplica la regla vigente (RN-12) con el
    /// multiplicador Gold (N1), guarda el lote como pendiente 15 días (N2) y devuelve el saldo.
    /// </summary>
    public async Task<AcumulacionResponseDTO> RegistrarAcumulacionAsync(AcumulacionCreateDTO dto)
    {
        // 1. Formato del documento, antes de ir a la base (fail fast, flujo 3b).
        if (!FormatoDocumento.IsMatch(dto.ClienteDocumento))
        {
            throw new DocumentoInvalidoException("El documento ingresado fue incorrecto.");
        }

        // 2. El cliente tiene que existir (3a) y estar activo (RN-14).
        var cliente = await clienteRepository.GetByDocumentoAsync(dto.ClienteDocumento)
            ?? throw new ClienteNotFoundException("El documento ingresado no se encuentra registrado.");

        if (!cliente.Activo)
        {
            throw new CuentaInactivaException("El cliente se encuentra inactivo.");
        }

        // 3. Monto de la compra con los precios reales de los productos (2a, RN-26).
        var montoTotal = await CalcularMontoAsync(dto.Items);

        // 4. Regla vigente hoy (RN-12) y multiplicador por categoría (N1).
        var ahora = DateTime.UtcNow;
        var regla = await reglaRepository.GetVigenteAsync(ahora)
            ?? throw new ReglaAcumulacionNoVigenteException("No hay una regla de acumulación vigente.");

        var multiplicador = cliente.Categoria == CategoriaCliente.Gold ? MultiplicadorGold : 1m;
        var puntos = (int)Math.Floor(montoTotal * regla.PuntosPorMonto * multiplicador);

        // 5. El lote queda pendiente 15 días (N2) y vence `DiasVigenciaPuntos` después de estar disponible (RN-21).
        var fechaDisponible = ahora.AddDays(DiasPendiente);
        var movimiento = new Movimiento
        {
            ClienteId = cliente.Id,
            Tipo = TipoMovimiento.Acumulacion,
            Puntos = puntos,
            PuntosDisponibles = puntos,
            Fecha = ahora,
            FechaDisponible = fechaDisponible,
            FechaVencimiento = fechaDisponible.AddDays(regla.DiasVigenciaPuntos),
            // Invariant: el detalle se guarda igual sin importar la configuración regional del servidor.
            Detalle = FormattableString.Invariant($"Compra por ${montoTotal} ({regla.PuntosPorMonto} pts/$ x{multiplicador})")
        };

        // 6. Persistir (RN-13) y leer el saldo; una falla técnica se traduce a 500 (flujo 4a).
        int saldoDisponible;
        try
        {
            movimiento = await movimientoRepository.CreateAsync(movimiento);
            var lotes = await movimientoRepository.GetLotesVigentesPorClienteAsync(cliente.Id, ahora);
            saldoDisponible = lotes.Sum(l => l.PuntosDisponibles);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("No se pudo registrar la acumulación en este momento.", ex);
        }

        return new AcumulacionResponseDTO
        {
            MovimientoId = movimiento.Id,
            ClienteId = cliente.Id,
            MontoTotal = montoTotal,
            PuntosPorMonto = regla.PuntosPorMonto,
            Multiplicador = multiplicador,
            PuntosAcreditados = puntos,
            FechaDisponible = fechaDisponible,
            FechaVencimiento = movimiento.FechaVencimiento!.Value,
            SaldoDisponible = saldoDisponible
        };
    }

    /// <summary>
    /// Suma precio x cantidad de cada ítem. Si un producto no existe o está inactivo (RN-26)
    /// rechaza la compra indicando qué ítem está mal, para que el empleado lo corrija (2a).
    /// </summary>
    private async Task<decimal> CalcularMontoAsync(List<ItemCompraDTO> items)
    {
        decimal total = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var producto = await productoRepository.GetByIdAsync(item.ProductoId!.Value)
                ?? throw new ItemCompraInvalidoException($"Ítem {i + 1}: el producto no existe.");

            if (!producto.Activo)
            {
                throw new ItemCompraInvalidoException($"Ítem {i + 1}: el producto '{producto.Nombre}' está inactivo.");
            }

            total += producto.Precio * item.Cantidad!.Value;
        }

        return total;
    }
}
