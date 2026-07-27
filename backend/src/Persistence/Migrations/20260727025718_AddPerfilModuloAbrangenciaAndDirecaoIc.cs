using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilModuloAbrangenciaAndDirecaoIc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfilModuloAbrangencia",
                schema: "public",
                columns: table => new
                {
                    PerfilId = table.Column<Guid>(type: "uuid", nullable: false),
                    Modulo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Abrangencia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilModuloAbrangencia", x => new { x.PerfilId, x.Modulo });
                    table.ForeignKey(
                        name: "FK_PerfilModuloAbrangencia_Perfil_PerfilId",
                        column: x => x.PerfilId,
                        principalSchema: "public",
                        principalTable: "Perfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfilModuloAbrangencia",
                schema: "public");
        }
    }
}
