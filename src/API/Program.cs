using System.Text;
using FidelixAPI.BusinessLogic.Interfaces;
using FidelixAPI.BusinessLogic.Jobs;
using FidelixAPI.BusinessLogic.Services;
using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Repositories;
using FidelixAPI.DataAccess.Repositories.Interfaces;
using FidelixAPI.DataAccess.Seed;
using FidelixAPI.Shared.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DataAccess: EF Core + SQLite (RNF-04: arquitectura en capas; Fase 1 de la adenda).
builder.Services.AddDbContext<FidelixDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FidelixDb")));

// Repositorios (DataAccess): inyectados por interfaz, nunca instanciados por BusinessLogic.
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IBeneficioRepository, BeneficioRepository>();
builder.Services.AddScoped<IReglaAcumulacionRepository, ReglaAcumulacionRepository>();
builder.Services.AddScoped<IMovimientoRepository, MovimientoRepository>();
builder.Services.AddScoped<ICodigoRecuperacionRepository, CodigoRecuperacionRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

// Servicios (BusinessLogic).
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IClienteAdminService, ClienteAdminService>();
builder.Services.AddScoped<IEmpleadoAdminService, EmpleadoAdminService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IBeneficioService, BeneficioService>();
builder.Services.AddScoped<IReglaAcumulacionService, ReglaAcumulacionService>();
builder.Services.AddScoped<IPuntosService, PuntosService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<ICanjeService, CanjeService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

// Jobs de Sistema (CU-22, CU-26): corren en segundo plano, una vez por día.
// FidelixApiFactory (tests de integración) los remueve para no correr
// trabajo real en cada arranque de la suite.
builder.Services.AddHostedService<VencimientoPuntosJob>();
builder.Services.AddHostedService<BonoCumpleanosJob>();

// JWT: configuración fuertemente tipada + autenticación (login unificado, ver AuthService).
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Documentación interactiva en /scalar/v1
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expone el Program implícito de los top-level statements para que
// `WebApplicationFactory<Program>` (tests de integración) pueda referenciarlo.
public partial class Program;
