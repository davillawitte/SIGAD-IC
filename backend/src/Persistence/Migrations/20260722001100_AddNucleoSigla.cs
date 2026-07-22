using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNucleoSigla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sigla",
                schema: "public",
                table: "Nucleo",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Nucleo"
                SET "Sigla" = upper(left(regexp_replace(trim("Nome"), '[^A-Za-z0-9ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç]', '', 'g'), 8));

                UPDATE "Nucleo"
                SET "Sigla" = 'NUC' || upper(left(replace("Id"::text, '-', ''), 5))
                WHERE "Sigla" IS NULL OR trim("Sigla") = '';

                WITH ranked AS (
                    SELECT "Id",
                           "Sigla",
                           row_number() OVER (PARTITION BY upper("Sigla") ORDER BY "CreatedAt", "Id") AS rn
                    FROM "Nucleo"
                )
                UPDATE "Nucleo" n
                SET "Sigla" = ranked."Sigla" || ranked.rn::text
                FROM ranked
                WHERE n."Id" = ranked."Id"
                  AND ranked.rn > 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Sigla",
                schema: "public",
                table: "Nucleo",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nucleo_Sigla",
                schema: "public",
                table: "Nucleo",
                column: "Sigla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nucleo_Sigla",
                schema: "public",
                table: "Nucleo");

            migrationBuilder.DropColumn(
                name: "Sigla",
                schema: "public",
                table: "Nucleo");
        }
    }
}
