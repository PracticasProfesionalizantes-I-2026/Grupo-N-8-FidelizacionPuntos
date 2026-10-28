using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Entities;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.Shared.DTOs.Empleado;
using FidelixAPI.Shared.Exceptions;
using FidelixAPI.Shared.Security;
using Moq;
using Xunit;

namespace FidelixAPI.BusinessLogic.Tests.Services;

/// <summary> Tests del ABM de empleados por el admin (CU-16). </summary>
public class EmpleadoAdminServiceTests
{
    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly EmpleadoAdminService _sut;

    /// <summary> Arma el service con el repositorio mockeado. </summary>
    public EmpleadoAdminServiceTests() => _sut = new EmpleadoAdminService(_empleadoRepo.Object);

    /// <summary> Datos válidos de un empleado nuevo. </summary>
    private static EmpleadoCreateDTO Nuevo() => new() { Nombre = "Emp", Documento = "30999888", Email = "emp@test.com", Password = "Empleado123!" };

    /// <summary> CU-16, alta: crea el empleado activo y guarda el hash, nunca la contraseña. </summary>
    [Fact]
    public async Task CrearEmpleadoAsync_WithValidData_CreatesAndReturnsEmpleado()
    {
        Empleado? creado = null;
        _empleadoRepo.Setup(r => r.CreateAsync(It.IsAny<Empleado>())).ReturnsAsync((Empleado e) => { creado = e; return e; });

        var resultado = await _sut.CrearEmpleadoAsync(Nuevo());

        Assert.True(resultado.Activo);
        Assert.True(PasswordHasher.Verify("Empleado123!", creado!.PasswordHash));
    }

    /// <summary> CU-16, flujo 3a: un documento ya registrado lanza EmpleadoDuplicadoException (RN-01). </summary>
    [Fact]
    public async Task CrearEmpleadoAsync_WhenDocumentoExists_ThrowsEmpleadoDuplicadoException()
    {
        _empleadoRepo.Setup(r => r.GetByDocumentoAsync("30999888")).ReturnsAsync(new Empleado());

        await Assert.ThrowsAsync<EmpleadoDuplicadoException>(() => _sut.CrearEmpleadoAsync(Nuevo()));
        _empleadoRepo.Verify(r => r.CreateAsync(It.IsAny<Empleado>()), Times.Never);
    }

    /// <summary> CU-16, RN-02: una contraseña débil se rechaza antes de consultar la base. </summary>
    [Fact]
    public async Task CrearEmpleadoAsync_WithWeakPassword_ThrowsPasswordInvalidaException()
    {
        var dto = Nuevo();
        dto.Password = "123";

        await Assert.ThrowsAsync<PasswordInvalidaException>(() => _sut.CrearEmpleadoAsync(dto));
        _empleadoRepo.Verify(r => r.GetByDocumentoAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary> CU-16, modificación parcial: cambia lo enviado. </summary>
    [Fact]
    public async Task ActualizarEmpleadoAsync_WithValidData_UpdatesAndReturnsEmpleado()
    {
        var empleado = new Empleado { Id = Guid.NewGuid(), Nombre = "Emp", Email = "emp@test.com" };
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id)).ReturnsAsync(empleado);

        var resultado = await _sut.ActualizarEmpleadoAsync(empleado.Id, new EmpleadoUpdateDTO { Nombre = "Empleado Nuevo" });

        Assert.Equal("Empleado Nuevo", resultado.Nombre);
        Assert.Equal("emp@test.com", resultado.Email);
    }

    /// <summary> CU-16, flujo 3b: un id inexistente lanza EmpleadoNotFoundException. </summary>
    [Fact]
    public async Task ActualizarEmpleadoAsync_WithNonExistentId_ThrowsEmpleadoNotFoundException()
    {
        _empleadoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Empleado?)null);

        await Assert.ThrowsAsync<EmpleadoNotFoundException>(() =>
            _sut.ActualizarEmpleadoAsync(Guid.NewGuid(), new EmpleadoUpdateDTO { Nombre = "X" }));
    }

    /// <summary> CU-16, baja: el empleado queda inactivo y pierde el acceso (RN-15). </summary>
    [Fact]
    public async Task BajaEmpleadoAsync_WithExistingId_DeactivatesEmpleado()
    {
        var empleado = new Empleado { Id = Guid.NewGuid(), Activo = true };
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id)).ReturnsAsync(empleado);

        await _sut.BajaEmpleadoAsync(empleado.Id);

        Assert.False(empleado.Activo);
        _empleadoRepo.Verify(r => r.UpdateAsync(empleado), Times.Once);
    }
}
