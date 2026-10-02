using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelixAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarBloqueoLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoHasta",
                table: "Clientes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloqueadoHasta",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "Clientes");
        }
    }
}
