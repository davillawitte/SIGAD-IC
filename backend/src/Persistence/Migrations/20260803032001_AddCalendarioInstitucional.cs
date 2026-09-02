using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarioInstitucional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarioAno",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PublicadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicadoPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioAno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarcacaoCalendario",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarioAnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Origem = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Local = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublicoAlvo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcacaoCalendario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarcacaoCalendario_CalendarioAno_CalendarioAnoId",
                        column: x => x.CalendarioAnoId,
                        principalSchema: "public",
                        principalTable: "CalendarioAno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioAno_Ano",
                schema: "public",
                table: "CalendarioAno",
                column: "Ano",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarcacaoCalendario_CalendarioAnoId_Data",
                schema: "public",
                table: "MarcacaoCalendario",
                columns: new[] { "CalendarioAnoId", "Data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarcacaoCalendario",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CalendarioAno",
                schema: "public");
        }
    }
}
