using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelixAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPersonalAuditoriaYRecuperacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmpleadoId",
                table: "Movimientos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoCanje",
                table: "Movimientos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoOperacion = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ActorRol = table.Column<string>(type: "TEXT", nullable: false),
                    EntidadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Detalle = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodigosRecuperacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    CodigoHash = table.Column<string>(type: "TEXT", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Usado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosRecuperacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Documento = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosRecuperacion_Email",
                table: "CodigosRecuperacion",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Documento",
                table: "Empleados",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Email",
                table: "Empleados",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "CodigosRecuperacion");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropColumn(
                name: "EmpleadoId",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "EstadoCanje",
                table: "Movimientos");
        }
    }
}
