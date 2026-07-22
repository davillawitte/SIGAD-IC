using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNucleoSetorAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropColumn(
                name: "Ativo",
                schema: "public",
                table: "Nucleo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                schema: "public",
                table: "Setor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                schema: "public",
                table: "Nucleo",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
