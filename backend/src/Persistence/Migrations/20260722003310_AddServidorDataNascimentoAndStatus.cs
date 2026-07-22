using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServidorDataNascimentoAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "public",
                table: "Servidor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataNascimento",
                schema: "public",
                table: "Servidor",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Servidor"
                SET "Status" = CASE WHEN "Ativo" THEN 'Ativo' ELSE 'Afastado' END,
                    "DataNascimento" = DATE '1990-01-01'
                WHERE "Status" IS NULL;

                -- Matrículas só com dígitos → formato xxx.xxx-x / xx.xxx-x / x.xxx-x
                UPDATE "Servidor"
                SET "Matricula" =
                    CASE length(regexp_replace("Matricula", '\D', '', 'g'))
                        WHEN 7 THEN
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 1 for 3) || '.' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 4 for 3) || '-' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 7 for 1)
                        WHEN 6 THEN
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 1 for 2) || '.' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 3 for 3) || '-' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 6 for 1)
                        WHEN 5 THEN
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 1 for 1) || '.' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 2 for 3) || '-' ||
                            substring(regexp_replace("Matricula", '\D', '', 'g') from 5 for 1)
                        ELSE "Matricula"
                    END
                WHERE "Matricula" !~ '^\d{1,3}\.\d{3}-\d$';
                """);

            migrationBuilder.DropColumn(
                name: "Ativo",
                schema: "public",
                table: "Servidor");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "Servidor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DataNascimento",
                schema: "public",
                table: "Servidor",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servidor_Status",
                schema: "public",
                table: "Servidor",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Servidor_Status",
                schema: "public",
                table: "Servidor");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                schema: "public",
                table: "Servidor",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE "Servidor"
                SET "Ativo" = ("Status" = 'Ativo');
                """);

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "public",
                table: "Servidor");
        }
    }
}
