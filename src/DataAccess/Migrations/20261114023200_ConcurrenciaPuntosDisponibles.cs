using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelixAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrenciaPuntosDisponibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vacía a propósito: marcar PuntosDisponibles como token de concurrencia (CRIT-01) no cambia
            // el esquema de SQLite, solo el modelo de EF. La migración existe para actualizar el snapshot
            // (sin ella, EF detecta cambios de modelo pendientes y no aplica las migraciones al arrancar).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
