using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests del algoritmo FIFO (RN-09, CU-23) que usa el canje (CU-07). No hay mocks: es un cálculo puro. </summary>
public class LotePuntosServiceTests
{
    private readonly LotePuntosService _sut = new();

    /// <summary> RN-09: descuenta del lote más antiguo aunque venga último en la lista. </summary>
    [Fact]
    public void AplicarFifo_DiscountsOldestLotFirst()
    {
        var nuevo = new Movimiento { Fecha = new DateTime(2026, 10, 1), PuntosDisponibles = 50 };
        var viejo = new Movimiento { Fecha = new DateTime(2026, 9, 1), PuntosDisponibles = 100 };

        _sut.AplicarFifo([nuevo, viejo], 120);

        Assert.Equal(0, viejo.PuntosDisponibles);
        Assert.Equal(30, nuevo.PuntosDisponibles);
    }

    /// <summary> Si el primer lote alcanza, los siguientes no se tocan. </summary>
    [Fact]
    public void AplicarFifo_WhenFirstLotIsEnough_LeavesOthersIntact()
    {
        var viejo = new Movimiento { Fecha = new DateTime(2026, 9, 1), PuntosDisponibles = 100 };
        var nuevo = new Movimiento { Fecha = new DateTime(2026, 10, 1), PuntosDisponibles = 50 };

        _sut.AplicarFifo([viejo, nuevo], 40);

        Assert.Equal(60, viejo.PuntosDisponibles);
        Assert.Equal(50, nuevo.PuntosDisponibles);
    }

    /// <summary> Defensa: si los lotes no alcanzan, es un error de programación (el saldo se verifica antes). </summary>
    [Fact]
    public void AplicarFifo_WhenLotsAreNotEnough_Throws()
    {
        var lote = new Movimiento { Fecha = new DateTime(2026, 9, 1), PuntosDisponibles = 10 };

        Assert.Throws<InvalidOperationException>(() => _sut.AplicarFifo([lote], 50));
    }
}
