using FidelixAPI.DataAccess.Context;
using FidelixAPI.DataAccess.Seed;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DataAccess: EF Core + SQLite (RNF-04: arquitectura en capas; Fase 1 de la adenda).
builder.Services.AddDbContext<FidelixDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FidelixDb")));

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

app.UseAuthorization();

app.MapControllers();

app.Run();
