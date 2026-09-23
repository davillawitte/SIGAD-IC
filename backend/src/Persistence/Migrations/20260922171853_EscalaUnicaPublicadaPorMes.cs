using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscalaUnicaPublicadaPorMes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "NucleoId", "Ano", "Mes" },
                unique: true,
                filter: "\"Status\" = 'Publicada'");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "SetorId", "Ano", "Mes" },
                unique: true,
                filter: "\"Status\" = 'Publicada'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.DropIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_NucleoId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "NucleoId", "Ano", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId_Ano_Mes",
                schema: "public",
                table: "Escala",
                columns: new[] { "SetorId", "Ano", "Mes" },
                unique: true);
        }
    }
}
