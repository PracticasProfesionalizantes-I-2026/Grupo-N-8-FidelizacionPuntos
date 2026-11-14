using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FidelixAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IntentosCodigoRecuperacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "CodigosRecuperacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "CodigosRecuperacion");
        }
    }
}
