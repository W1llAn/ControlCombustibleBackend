using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MicroservicioAsignacionRutas.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionRutas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ChoferId = table.Column<int>(type: "integer", nullable: false),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
                    RutaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionRutas", x => x.id);
                    table.ForeignKey(
                        name: "FK_AsignacionRutas_Choferes_ChoferId",
                        column: x => x.ChoferId,
                        principalTable: "Choferes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionRutas_Rutas_RutaId",
                        column: x => x.RutaId,
                        principalTable: "Rutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionRutas_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionRutas_ChoferId",
                table: "AsignacionRutas",
                column: "ChoferId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionRutas_RutaId",
                table: "AsignacionRutas",
                column: "RutaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionRutas_VehiculoId",
                table: "AsignacionRutas",
                column: "VehiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionRutas");
        }
    }
}
