using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroservicioVehiculos.Migrations
{
    /// <inheritdoc />
    public partial class nombredescripcionvehiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "Vehiculos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nombre",
                table: "Vehiculos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "nombre",
                table: "Vehiculos");
        }
    }
}
