using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenciaCicloJornada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SequenciaCiclo",
                schema: "public",
                table: "PadraoEscala",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SequenciaCiclo",
                schema: "public",
                table: "EscalaJornada",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenciaCiclo",
                schema: "public",
                table: "PadraoEscala");

            migrationBuilder.DropColumn(
                name: "SequenciaCiclo",
                schema: "public",
                table: "EscalaJornada");
        }
    }
}
