using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalaResumidaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscalaResumida",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NucleoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PublicadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicadaPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaResumida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaResumida_Nucleo_NucleoId",
                        column: x => x.NucleoId,
                        principalSchema: "public",
                        principalTable: "Nucleo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalaResumidaSetor",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    SetorNomeSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SetorSiglaSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaResumidaSetor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaSetor_EscalaResumida_EscalaResumidaId",
                        column: x => x.EscalaResumidaId,
                        principalSchema: "public",
                        principalTable: "EscalaResumida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaSetor_Setor_SetorId",
                        column: x => x.SetorId,
                        principalSchema: "public",
                        principalTable: "Setor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoDevolucaoEscalaResumida",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "EscalaResumidaEquipe",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaSetorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    DataInicioCiclo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaResumidaEquipe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaEquipe_EscalaResumidaSetor_EscalaResumidaSeto~",
                        column: x => x.EscalaResumidaSetorId,
                        principalSchema: "public",
                        principalTable: "EscalaResumidaSetor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscalaResumidaRotacaoMembro",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaEquipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Posicao = table.Column<int>(type: "integer", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaResumidaRotacaoMembro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaRotacaoMembro_EscalaResumidaEquipe_EscalaResu~",
                        column: x => x.EscalaResumidaEquipeId,
                        principalSchema: "public",
                        principalTable: "EscalaResumidaEquipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaRotacaoMembro_Servidor_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalaResumidaDia",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaResumidaEquipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServidorNomeSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TextoLivre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsFolga = table.Column<bool>(type: "boolean", nullable: false),
                    Origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RotacaoMembroId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaResumidaDia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaDia_EscalaResumidaEquipe_EscalaResumidaEquipe~",
                        column: x => x.EscalaResumidaEquipeId,
                        principalSchema: "public",
                        principalTable: "EscalaResumidaEquipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaDia_EscalaResumidaRotacaoMembro_RotacaoMembro~",
                        column: x => x.RotacaoMembroId,
                        principalSchema: "public",
                        principalTable: "EscalaResumidaRotacaoMembro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EscalaResumidaDia_Servidor_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_NucleoId",
                schema: "public",
                table: "EscalaResumida",
                column: "NucleoId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_NucleoId_Ano_Mes",
                schema: "public",
                table: "EscalaResumida",
                columns: new[] { "NucleoId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumida_Status",
                schema: "public",
                table: "EscalaResumida",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaDia_Data",
                schema: "public",
                table: "EscalaResumidaDia",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaDia_EscalaResumidaEquipeId_Data",
                schema: "public",
                table: "EscalaResumidaDia",
                columns: new[] { "EscalaResumidaEquipeId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaDia_RotacaoMembroId",
                schema: "public",
                table: "EscalaResumidaDia",
                column: "RotacaoMembroId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaDia_ServidorId",
                schema: "public",
                table: "EscalaResumidaDia",
                column: "ServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaEquipe_EscalaResumidaSetorId",
                schema: "public",
                table: "EscalaResumidaEquipe",
                column: "EscalaResumidaSetorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaRotacaoMembro_EscalaResumidaEquipeId_Posicao",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro",
                columns: new[] { "EscalaResumidaEquipeId", "Posicao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaRotacaoMembro_ServidorId",
                schema: "public",
                table: "EscalaResumidaRotacaoMembro",
                column: "ServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaSetor_EscalaResumidaId_SetorId",
                schema: "public",
                table: "EscalaResumidaSetor",
                columns: new[] { "EscalaResumidaId", "SetorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaResumidaSetor_SetorId",
                schema: "public",
                table: "EscalaResumidaSetor",
                column: "SetorId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalaResumidaDia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SolicitacaoDevolucaoEscalaResumida",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaResumidaRotacaoMembro",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaResumidaEquipe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaResumidaSetor",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaResumida",
                schema: "public");
        }
    }
}
