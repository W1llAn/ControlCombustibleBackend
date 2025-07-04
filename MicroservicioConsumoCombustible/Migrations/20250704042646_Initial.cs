using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MicroservicioConsumoCombustible.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumoCombustibles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    combustibleEstimado = table.Column<decimal>(type: "numeric", nullable: false),
                    combustibleReal = table.Column<decimal>(type: "numeric", nullable: false),
                    fechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    idAsignacionRuta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumoCombustibles", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConsumoCombustibles_AsignacionRutas_idAsignacionRuta",
                        column: x => x.idAsignacionRuta,
                        principalTable: "AsignacionRutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoCombustibles_idAsignacionRuta",
                table: "ConsumoCombustibles",
                column: "idAsignacionRuta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumoCombustibles");
        }
    }
}
