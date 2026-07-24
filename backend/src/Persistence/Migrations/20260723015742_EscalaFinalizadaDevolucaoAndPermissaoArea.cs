using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscalaFinalizadaDevolucaoAndPermissaoArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                schema: "public",
                table: "Permissao",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadaEm",
                schema: "public",
                table: "Escala",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicadaPor",
                schema: "public",
                table: "Escala",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Escala"
                SET "Status" = 'Finalizada'
                WHERE "Status" = 'Encerrada';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Permissao"
                SET "Area" = CASE
                    WHEN "Modulo" IN ('usuarios', 'perfis', 'permissoes') THEN 'Administração do Sistema'
                    WHEN "Modulo" IN ('escalas') THEN 'Gestão do Setor'
                    ELSE 'Gestão Institucional'
                END
                WHERE "Area" = '' OR "Area" IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "SolicitacaoDevolucaoEscala",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitanteUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Justificativa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RespondidoPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RespostaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ObservacaoResposta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoDevolucaoEscala", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoDevolucaoEscala_Escala_EscalaId",
                        column: x => x.EscalaId,
                        principalSchema: "public",
                        principalTable: "Escala",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitacaoDevolucaoEscala_Usuario_SolicitanteUsuarioId",
                        column: x => x.SolicitanteUsuarioId,
                        principalSchema: "public",
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissao_Area",
                schema: "public",
                table: "Permissao",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscala_EscalaId",
                schema: "public",
                table: "SolicitacaoDevolucaoEscala",
                column: "EscalaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscala_EscalaId_Status",
                schema: "public",
                table: "SolicitacaoDevolucaoEscala",
                columns: new[] { "EscalaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscala_SolicitanteUsuarioId",
                schema: "public",
                table: "SolicitacaoDevolucaoEscala",
                column: "SolicitanteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoDevolucaoEscala_Status",
                schema: "public",
                table: "SolicitacaoDevolucaoEscala",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacaoDevolucaoEscala",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Permissao_Area",
                schema: "public",
                table: "Permissao");

            migrationBuilder.DropColumn(
                name: "Area",
                schema: "public",
                table: "Permissao");

            migrationBuilder.DropColumn(
                name: "PublicadaEm",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropColumn(
                name: "PublicadaPor",
                schema: "public",
                table: "Escala");
        }
    }
}
