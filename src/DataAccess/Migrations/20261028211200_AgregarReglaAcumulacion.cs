using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelixAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReglaAcumulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReglasAcumulacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PuntosPorMonto = table.Column<decimal>(type: "TEXT", nullable: false),
                    VigenciaDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DiasVigenciaPuntos = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasAcumulacion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReglasAcumulacion_VigenciaDesde",
                table: "ReglasAcumulacion",
                column: "VigenciaDesde",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReglasAcumulacion");
        }
    }
}
