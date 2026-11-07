using FidelixAPI.BusinessLogic.Helpers;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Comun;
using FidelixAPI.Shared.DTOs.Movimiento;
using FidelixAPI.Shared.Enums;
using FidelixAPI.Shared.Exceptions;

namespace FidelixAPI.BusinessLogic.Services;

/// <summary> Implementación de IMovimientoService (CU-10, CU-20). </summary>
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

    /// <summary>
    /// Registra la compra: busca al cliente por su id (3a), calcula el monto con
    /// los precios de los productos (2a, RN-26), aplica la regla vigente (RN-12) con el
    /// multiplicador Gold (N1), guarda el lote como pendiente 15 días (N2) y devuelve el saldo.
    /// </summary>
    public async Task<AcumulacionResponseDTO> RegistrarAcumulacionAsync(int clienteId, AcumulacionCreateDTO dto, int? empleadoId = null)
    {
        // 1. El cliente tiene que existir (3a) y estar activo (RN-14). Se identifica por el id que llega
        //    en la ruta: el DNI solo se usa antes, en la búsqueda (`ClienteService.BuscarPorDocumentoAsync`).
        var cliente = await clienteRepository.GetByIdAsync(clienteId)
            ?? throw new ClienteNotFoundException("El cliente no se encuentra registrado.");

        if (!cliente.Activo)
        {
            throw new CuentaInactivaException("El cliente se encuentra inactivo.");
        }

        // 2. Monto de la compra con los precios reales de los productos (2a, RN-26).
        var montoTotal = await CalcularMontoAsync(dto.Items);

        // 3. Regla vigente hoy (RN-12) y multiplicador por categoría (N1).
        var ahora = DateTime.UtcNow;
        var regla = await reglaRepository.GetVigenteAsync(ahora)
            ?? throw new ReglaAcumulacionNoVigenteException("No hay una regla de acumulación vigente.");

        var multiplicador = cliente.Categoria == CategoriaCliente.Gold ? MultiplicadorGold : 1m;
        var puntos = (int)Math.Floor(montoTotal * regla.PuntosPorMonto * multiplicador);

        // 4. El lote queda pendiente 15 días (N2) y vence `DiasVigenciaPuntos` después de estar disponible (RN-21).
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
            EmpleadoId = empleadoId,
            // Invariant: el detalle se guarda igual sin importar la configuración regional del servidor.
            Detalle = FormattableString.Invariant($"Compra por ${montoTotal} ({regla.PuntosPorMonto} pts/$ x{multiplicador})")
        };

        // 5. Persistir (RN-13) y leer el saldo; una falla técnica se traduce a 500 (flujo 4a).
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

    /// <summary> Consulta del admin (CU-20): todos los tipos de movimiento (RN-06); vacío no es error (4a). </summary>
    public async Task<ListadoResponseDTO<MovimientoResponseDTO>> ConsultarMovimientosAsync(MovimientoFiltroDTO filtro)
    {
        // Las fechas del filtro son hora Argentina y "hasta" sin hora incluye el día completo.
        var (desdeUtc, hastaUtc) = FechasNegocio.RangoUtc(filtro.FechaDesde, filtro.FechaHasta);
        var movimientos = await movimientoRepository.BuscarAsync(filtro.ClienteId, filtro.TipoMovimiento, desdeUtc, hastaUtc);
        var items = movimientos.Select(MovimientoMapper.ToResponseDTO).ToList();
        return ListadoResponseDTO<MovimientoResponseDTO>.Crear(items, "No existen resultados para los filtros seleccionados");
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
