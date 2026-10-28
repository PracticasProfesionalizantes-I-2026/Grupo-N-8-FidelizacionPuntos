using System.Text;
using FidelixAPI.API.Filters;
using FidelixAPI.API.Jobs;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Repositories;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.DataAccess.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// El filtro de auditoría registra toda operación que modifica datos (RN-13, CU-21).
builder.Services.AddControllers(options => options.Filters.Add<AuditoriaFilter>());
builder.Services.AddOpenApi();

// DataAccess: EF Core + SQLite (RNF-04: arquitectura en capas).
builder.Services.AddDbContext<FidelixDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FidelixDb")));

// Repositorios (DataAccess): inyectados por interfaz, nunca instanciados por BusinessLogic.
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IBeneficioRepository, BeneficioRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IReglaAcumulacionRepository, ReglaAcumulacionRepository>();
builder.Services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<ICodigoRecuperacionRepository, CodigoRecuperacionRepository>();

// Servicios (BusinessLogic): inyectados por interfaz en los controllers.
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IBeneficioService, BeneficioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IReglaAcumulacionService, ReglaAcumulacionService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<ICanjeService, CanjeService>();
builder.Services.AddScoped<ILotePuntosService, LotePuntosService>();
builder.Services.AddScoped<IAuthPersonalService, AuthPersonalService>();
builder.Services.AddScoped<IRecuperacionService, RecuperacionService>();
builder.Services.AddScoped<IClienteAdminService, ClienteAdminService>();
builder.Services.AddScoped<IEmpleadoAdminService, EmpleadoAdminService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IPuntosService, PuntosService>();
builder.Services.AddScoped<AuditoriaFilter>();

// Emails: en desarrollo se escriben en la consola (no hay servidor SMTP configurado).
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

// Procesos automáticos diarios: vencimiento de puntos (CU-22) y bono de cumpleaños (CU-26).
builder.Services.AddHostedService<PointsExpirationJob>();
builder.Services.AddHostedService<BonoCumpleanosJob>();

// Autenticación JWT (CU-02 emite el token; CU-07 lo exige). Valida firma, emisor,
// audiencia y vencimiento con los mismos valores de `Jwt` que usa JwtTokenService.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Falta la clave del token en la configuracion."))),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Aplica migraciones pendientes y carga datos de prueba si la base está vacía.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FidelixDbContext>();
    await DbInitializer.InicializarAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Documentación interactiva en /scalar/v1
}

app.UseHttpsRedirection();

// Primero se identifica quién llama (token) y después se decide si puede entrar ([Authorize]).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expone el Program implícito de los top-level statements para que
// `WebApplicationFactory<Program>` (tests de integración) pueda referenciarlo.
public partial class Program;
