using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalaNucleoLotacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SetorId",
                schema: "public",
                table: "Escala",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "NucleoId",
                schema: "public",
                table: "Escala",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Escala_NucleoId",
                schema: "public",
                table: "Escala",
                column: "NucleoId");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "NucleoId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Escala_Lotacao_SetorOuNucleo",
                schema: "public",
                table: "Escala",
                sql: "(\"SetorId\" IS NOT NULL AND \"NucleoId\" IS NULL) OR (\"SetorId\" IS NULL AND \"NucleoId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Escala_Nucleo_NucleoId",
                schema: "public",
                table: "Escala",
                column: "NucleoId",
                principalSchema: "public",
                principalTable: "Nucleo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Escala_Nucleo_NucleoId",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropIndex(
                name: "IX_Escala_NucleoId",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Escala_Lotacao_SetorOuNucleo",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "NucleoId",
                schema: "public",
                table: "Escala");

            migrationBuilder.AlterColumn<Guid>(
                name: "SetorId",
                schema: "public",
                table: "Escala",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
