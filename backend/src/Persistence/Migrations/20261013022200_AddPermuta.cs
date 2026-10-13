using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermuta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalaJornadaIdOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraFimOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraInicioOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HorasOriginais",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacaoOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemOriginal",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PermutaId",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Permutada",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Permuta",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitanteServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermutanteServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataIda = table.Column<DateOnly>(type: "date", nullable: false),
                    DataVolta = table.Column<DateOnly>(type: "date", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permuta_Servidor_PermutanteServidorId",
                        column: x => x.PermutanteServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permuta_Servidor_SolicitanteServidorId",
                        column: x => x.SolicitanteServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalaOcorrencia_Permutada",
                schema: "public",
                table: "EscalaOcorrencia",
                column: "Permutada");

            migrationBuilder.CreateIndex(
                name: "IX_Permuta_DataIda",
                schema: "public",
                table: "Permuta",
                column: "DataIda");

            migrationBuilder.CreateIndex(
                name: "IX_Permuta_DataVolta",
                schema: "public",
                table: "Permuta",
                column: "DataVolta");

            migrationBuilder.CreateIndex(
                name: "IX_Permuta_PermutanteServidorId",
                schema: "public",
                table: "Permuta",
                column: "PermutanteServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Permuta_SolicitanteServidorId",
                schema: "public",
                table: "Permuta",
                column: "SolicitanteServidorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Permuta",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_EscalaOcorrencia_Permutada",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "CodigoOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "EscalaJornadaIdOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "HoraFimOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "HoraInicioOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "HorasOriginais",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "ObservacaoOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "OrigemOriginal",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "PermutaId",
                schema: "public",
                table: "EscalaOcorrencia");

            migrationBuilder.DropColumn(
                name: "Permutada",
                schema: "public",
                table: "EscalaOcorrencia");
        }
    }
}
