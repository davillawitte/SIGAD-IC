using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServidorNucleoLotacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SetorId",
                schema: "public",
                table: "Servidor",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "NucleoId",
                schema: "public",
                table: "Servidor",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servidor_NucleoId",
                schema: "public",
                table: "Servidor",
                column: "NucleoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Servidor_Lotacao_SetorOuNucleo",
                schema: "public",
                table: "Servidor",
                sql: "(\"SetorId\" IS NOT NULL AND \"NucleoId\" IS NULL) OR (\"SetorId\" IS NULL AND \"NucleoId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Servidor_Nucleo_NucleoId",
                schema: "public",
                table: "Servidor",
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
                name: "FK_Servidor_Nucleo_NucleoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropIndex(
                name: "IX_Servidor_NucleoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Servidor_Lotacao_SetorOuNucleo",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropColumn(
                name: "NucleoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.AlterColumn<Guid>(
                name: "SetorId",
                schema: "public",
                table: "Servidor",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
