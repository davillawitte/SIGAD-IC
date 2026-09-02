using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalaResumidaSegundaPessoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFolga2",
                schema: "public",
                table: "EscalaResumidaDia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServidorNomeSnapshot2",
                schema: "public",
                table: "EscalaResumidaDia",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaRotacaoMembro_ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro",
                column: "ServidorId2");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaDia_ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia",
                column: "ServidorId2");

            migrationBuilder.AddForeignKey(
                name: "FK_EscalaResumidaDia_Servidor_ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia",
                column: "ServidorId2",
                principalSchema: "public",
                principalTable: "Servidor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EscalaResumidaRotacaoMembro_Servidor_ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro",
                column: "ServidorId2",
                principalSchema: "public",
                principalTable: "Servidor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscalaResumidaDia_Servidor_ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia");

            migrationBuilder.DropForeignKey(
                name: "FK_EscalaResumidaRotacaoMembro_Servidor_ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro");

            migrationBuilder.DropIndex(
                name: "IX_EscalaResumidaRotacaoMembro_ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro");

            migrationBuilder.DropIndex(
                name: "IX_EscalaResumidaDia_ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia");

            migrationBuilder.DropColumn(
                name: "ServidorId2",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro");

            migrationBuilder.DropColumn(
                name: "IsFolga2",
                schema: "public",
                table: "EscalaResumidaDia");

            migrationBuilder.DropColumn(
                name: "ServidorId2",
                schema: "public",
                table: "EscalaResumidaDia");

            migrationBuilder.DropColumn(
                name: "ServidorNomeSnapshot2",
                schema: "public",
                table: "EscalaResumidaDia");
        }
    }
}
