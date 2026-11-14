using FidelixAPI.BusinessLogic.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Helpers;

/// <summary>
/// Tests de <see cref="ClaveJwt"/> (CRIT-05): la clave de firma no está en el repositorio, así que
/// la API tiene que negarse a arrancar si falta o si es demasiado corta para HMAC-SHA256.
/// </summary>
public class ClaveJwtTests
{
    /// <summary> Arma una configuración falsa que devuelve el valor indicado para `Jwt:Key`. </summary>
    private static IConfiguration ConfigConClave(string? clave)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Jwt:Key"]).Returns(clave);
        return config.Object;
    }

    /// <summary> Una clave de 32 bytes o más se devuelve tal cual. </summary>
    [Fact]
    public void Obtener_WithLongEnoughKey_ReturnsKey()
    {
        var clave = new string('x', ClaveJwt.BytesMinimos);

        Assert.Equal(clave, ClaveJwt.Obtener(ConfigConClave(clave)));
    }

    /// <summary> Sin clave configurada (o vacía) falla, y el mensaje explica cómo cargarla con User Secrets. </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Obtener_WithoutKey_ThrowsWithInstructions(string? clave)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClaveJwt.Obtener(ConfigConClave(clave)));

        Assert.Contains("user-secrets", ex.Message);
    }

    /// <summary> Una clave de menos de 32 bytes es débil para HS256: se rechaza. </summary>
    [Fact]
    public void Obtener_WithShortKey_Throws()
    {
        var clave = new string('x', ClaveJwt.BytesMinimos - 1);

        var ex = Assert.Throws<InvalidOperationException>(() => ClaveJwt.Obtener(ConfigConClave(clave)));

        Assert.Contains("32 bytes", ex.Message);
    }
}
