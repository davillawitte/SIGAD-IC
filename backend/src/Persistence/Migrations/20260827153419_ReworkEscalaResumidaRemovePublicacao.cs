using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkEscalaResumidaRemovePublicacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacaoDevolucaoEscalaResumida",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "PublicadaEm",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropColumn(
                name: "PublicadaPor",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.AddColumn<Guid>(
                name: "EscalaId",
                schema: "public",
                table: "EscalaResumida",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_EscalaId",
                schema: "public",
                table: "EscalaResumida",
                column: "EscalaId");

            migrationBuilder.AddForeignKey(
                name: "FK_EscalaResumida_Escala_EscalaId",
                schema: "public",
                table: "EscalaResumida",
                column: "EscalaId",
                principalSchema: "public",
                principalTable: "Escala",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscalaResumida_Escala_EscalaId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropIndex(
                name: "IX_EscalaResumida_EscalaId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.DropColumn(
                name: "EscalaId",
                schema: "public",
                table: "EscalaResumida");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadaEm",
                schema: "public",
                table: "EscalaResumida",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicadaPor",
                schema: "public",
                table: "EscalaResumida",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolicitacaoDevolucaoEscalaResumida",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitanteUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Justificativa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ObservacaoResposta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RespondidoPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespostaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoDevolucaoEscalaResumida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoDevolucaoEscalaResumida_EscalaResumida_EscalaRes~",
                        column: x => x.EscalaResumidaId,
                        principalSchema: "public",
                        principalTable: "EscalaResumida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitacaoDevolucaoEscalaResumida_Usuario_SolicitanteUsuar~",
                        column: x => x.SolicitanteUsuarioId,
                        principalSchema: "public",
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscalaResumida_EscalaResumidaId",
                schema: "public",
                table: "SolicitacaoDevolucaoEscalaResumida",
                column: "EscalaResumidaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscalaResumida_EscalaResumidaId_Status",
                schema: "public",
                table: "SolicitacaoDevolucaoEscalaResumida",
                columns: new[] { "EscalaResumidaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscalaResumida_SolicitanteUsuarioId",
                schema: "public",
                table: "SolicitacaoDevolucaoEscalaResumida",
                column: "SolicitanteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscalaResumida_Status",
                schema: "public",
                table: "SolicitacaoDevolucaoEscalaResumida",
                column: "Status");
        }
    }
}
