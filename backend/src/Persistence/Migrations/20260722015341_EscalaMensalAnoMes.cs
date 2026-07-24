using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscalaMensalAnoMes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Escala_SetorId_DataInicio_DataFim",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "DataFim",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "DataInicio",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "Nome",
                schema: "public",
                table: "Escala");

            migrationBuilder.AddColumn<int>(
                name: "Ano",
                schema: "public",
                table: "Escala",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mes",
                schema: "public",
                table: "Escala",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "SetorId", "Ano", "Mes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "Ano",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFim",
                schema: "public",
                table: "Escala",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInicio",
                schema: "public",
                table: "Escala",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                schema: "public",
                table: "Escala",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId_DataInicio_DataFim",
                schema: "public",
                table: "Escala",
                columns: new[] { "SetorId", "DataInicio", "DataFim" });
        }
    }
}
