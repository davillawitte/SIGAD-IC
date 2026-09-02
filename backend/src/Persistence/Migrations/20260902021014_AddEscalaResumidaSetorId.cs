using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalaResumidaSetorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "NucleoId",
                schema: "public",
                table: "EscalaResumida",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SetorId",
                schema: "public",
                table: "EscalaResumida",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_SetorId",
                schema: "public",
                table: "EscalaResumida",
                column: "SetorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_SetorId_Ano_Mes",
                schema: "public",
                table: "EscalaResumida",
                columns: new[] { "SetorId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EscalaResumida_Setor_SetorId",
                schema: "public",
                table: "EscalaResumida",
                column: "SetorId",
                principalSchema: "public",
                principalTable: "Setor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscalaResumida_Setor_SetorId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropIndex(
                name: "IX_EscalaResumida_SetorId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropIndex(
                name: "IX_EscalaResumida_SetorId_Ano_Mes",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropColumn(
                name: "SetorId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.AlterColumn<Guid>(
                name: "NucleoId",
                schema: "public",
                table: "EscalaResumida",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
