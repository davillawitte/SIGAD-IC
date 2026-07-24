using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPadraoEscalaAndCicloContinuo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInicioCiclo",
                schema: "public",
                table: "EscalaJornada",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoFuncionamento",
                schema: "public",
                table: "Escala",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PadraoEscala",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TipoFuncionamento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TipoJornada = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecorrenciaTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DiasTrabalho = table.Column<int>(type: "integer", nullable: true),
                    DiasFolga = table.Column<int>(type: "integer", nullable: true),
                    DiasSemana = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TipoOcorrenciaTrabalho = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TipoOcorrenciaFolga = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HoraInicioPadrao = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HoraFimPadrao = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HorasPadrao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Sistema = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    SetorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PadraoEscala", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PadraoEscala_Setor_SetorId",
                        column: x => x.SetorId,
                        principalSchema: "public",
                        principalTable: "Setor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalaJornada_PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada",
                column: "PadraoEscalaId");

            migrationBuilder.CreateIndex(
                name: "IX_PadraoEscala_Codigo",
                schema: "public",
                table: "PadraoEscala",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PadraoEscala_SetorId",
                schema: "public",
                table: "PadraoEscala",
                column: "SetorId");

            migrationBuilder.CreateIndex(
                name: "IX_PadraoEscala_TipoFuncionamento",
                schema: "public",
                table: "PadraoEscala",
                column: "TipoFuncionamento");

            migrationBuilder.AddForeignKey(
                name: "FK_EscalaJornada_PadraoEscala_PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada",
                column: "PadraoEscalaId",
                principalSchema: "public",
                principalTable: "PadraoEscala",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscalaJornada_PadraoEscala_PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada");

            migrationBuilder.DropTable(
                name: "PadraoEscala",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_EscalaJornada_PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada");

            migrationBuilder.DropColumn(
                name: "DataInicioCiclo",
                schema: "public",
                table: "EscalaJornada");

            migrationBuilder.DropColumn(
                name: "PadraoEscalaId",
                schema: "public",
                table: "EscalaJornada");

            migrationBuilder.DropColumn(
                name: "TipoFuncionamento",
                schema: "public",
                table: "Escala");
        }
    }
}
