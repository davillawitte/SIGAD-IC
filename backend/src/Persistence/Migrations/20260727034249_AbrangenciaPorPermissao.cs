using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AbrangenciaPorPermissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Abrangencia",
                schema: "public",
                table: "PerfilPermissao",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Propaga a abrangência antiga (por módulo) para cada permissão daquele módulo.
            migrationBuilder.Sql("""
                UPDATE "PerfilPermissao" AS pp
                SET "Abrangencia" = pma."Abrangencia"
                FROM "PerfilModuloAbrangencia" AS pma, "Permissao" AS p
                WHERE pp."PerfilId" = pma."PerfilId"
                  AND p."Id" = pp."PermissaoId"
                  AND lower(p."Modulo") = pma."Modulo";
                """);

            migrationBuilder.DropTable(
                name: "PerfilModuloAbrangencia",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql("""
                INSERT INTO "PerfilModuloAbrangencia" ("PerfilId", "Modulo", "Abrangencia")
                SELECT pp."PerfilId", lower(p."Modulo"), MAX(pp."Abrangencia")
                FROM "PerfilPermissao" AS pp
                INNER JOIN "Permissao" AS p ON p."Id" = pp."PermissaoId"
                WHERE pp."Abrangencia" <> 1
                GROUP BY pp."PerfilId", lower(p."Modulo");
                """);

            migrationBuilder.DropColumn(
                name: "Abrangencia",
                schema: "public",
                table: "PerfilPermissao");
        }
    }
}
