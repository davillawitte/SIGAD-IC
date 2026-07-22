using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSetorChefia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Sigla",
                schema: "public",
                table: "Setor",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "SetorChefia",
                schema: "public",
                columns: table => new
                {
                    SetorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoChefia = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ServidorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetorChefia", x => new { x.SetorId, x.TipoChefia });
                    table.ForeignKey(
                        name: "FK_SetorChefia_Servidor_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SetorChefia_Setor_SetorId",
                        column: x => x.SetorId,
                        principalSchema: "public",
                        principalTable: "Setor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SetorChefia_ServidorId",
                schema: "public",
                table: "SetorChefia",
                column: "ServidorId");

            migrationBuilder.Sql("""
                INSERT INTO "SetorChefia" ("SetorId", "ServidorId", "TipoChefia")
                SELECT "Id", "ChefeServidorId",
                       CASE
                           WHEN "Codigo" = 'DIRECAO_IC'
                                OR "Id" = '11111111-1111-1111-1111-111111111111'
                               THEN 'Diretor'
                           ELSE 'ChefiaImediata'
                       END
                FROM "Setor"
                WHERE "ChefeServidorId" IS NOT NULL;

                INSERT INTO "SetorChefia" ("SetorId", "ServidorId", "TipoChefia")
                SELECT "Id", "ChefeSubstitutoServidorId",
                       CASE
                           WHEN "Codigo" = 'DIRECAO_IC'
                                OR "Id" = '11111111-1111-1111-1111-111111111111'
                               THEN 'Subcoordenador'
                           ELSE 'ChefiaSubstituta'
                       END
                FROM "Setor"
                WHERE "ChefeSubstitutoServidorId" IS NOT NULL;

                UPDATE "Setor"
                SET "Sigla" = 'Direção IC',
                    "Nome" = 'Direção do Instituto de Criminalística'
                WHERE "Codigo" = 'DIRECAO_IC'
                   OR "Id" = '11111111-1111-1111-1111-111111111111';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Setor_Servidor_ChefeServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropForeignKey(
                name: "FK_Setor_Servidor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropIndex(
                name: "IX_Setor_ChefeServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropIndex(
                name: "IX_Setor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropIndex(
                name: "IX_Setor_Codigo",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropColumn(
                name: "ChefeServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropColumn(
                name: "ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropColumn(
                name: "Codigo",
                schema: "public",
                table: "Setor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChefeServidorId",
                schema: "public",
                table: "Setor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                schema: "public",
                table: "Setor",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Setor" s
                SET "ChefeServidorId" = sc."ServidorId"
                FROM "SetorChefia" sc
                WHERE sc."SetorId" = s."Id"
                  AND sc."TipoChefia" IN ('Diretor', 'ChefiaImediata');

                UPDATE "Setor" s
                SET "ChefeSubstitutoServidorId" = sc."ServidorId"
                FROM "SetorChefia" sc
                WHERE sc."SetorId" = s."Id"
                  AND sc."TipoChefia" IN ('Subcoordenador', 'ChefiaSubstituta');

                UPDATE "Setor"
                SET "Codigo" = CASE
                    WHEN "Sigla" = 'Direção IC' OR "Id" = '11111111-1111-1111-1111-111111111111'
                        THEN 'DIRECAO_IC'
                    ELSE upper(replace(trim("Sigla"), ' ', '_'))
                END;
                """);

            migrationBuilder.DropTable(
                name: "SetorChefia",
                schema: "public");

            migrationBuilder.AlterColumn<string>(
                name: "Sigla",
                schema: "public",
                table: "Setor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "IX_Setor_ChefeServidorId",
                schema: "public",
                table: "Setor",
                column: "ChefeServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Setor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor",
                column: "ChefeSubstitutoServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Setor_Codigo",
                schema: "public",
                table: "Setor",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Setor_Servidor_ChefeServidorId",
                schema: "public",
                table: "Setor",
                column: "ChefeServidorId",
                principalSchema: "public",
                principalTable: "Servidor",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Setor_Servidor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor",
                column: "ChefeSubstitutoServidorId",
                principalSchema: "public",
                principalTable: "Servidor",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
