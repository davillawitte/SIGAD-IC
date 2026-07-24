using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAfastamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Afastamento",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoOcorrenciaCodigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afastamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Afastamento_Servidor_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afastamento_DataInicio_DataFim",
                schema: "public",
                table: "Afastamento",
                columns: new[] { "DataInicio", "DataFim" });

            migrationBuilder.CreateIndex(
                name: "IX_Afastamento_ServidorId",
                schema: "public",
                table: "Afastamento",
                column: "ServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Afastamento_TipoOcorrenciaCodigo",
                schema: "public",
                table: "Afastamento",
                column: "TipoOcorrenciaCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Afastamento",
                schema: "public");
        }
    }
}
