using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNucleoAndEvolveSetor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NucleoId",
                schema: "public",
                table: "Setor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resumo",
                schema: "public",
                table: "Setor",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Setor seed da Diretoria → Direção do IC
            migrationBuilder.Sql("""
                UPDATE "Setor"
                SET "Codigo" = 'DIRECAO_IC',
                    "Nome" = 'Direção do Instituto de Criminalística',
                    "Sigla" = 'DIC',
                    "Resumo" = 'Direção geral do Instituto de Criminalística',
                    "NucleoId" = NULL
                WHERE "Id" = '11111111-1111-1111-1111-111111111111'
                   OR upper(trim("Sigla")) = 'DIR';
                """);

            // Demais setores legados: código a partir da sigla
            migrationBuilder.Sql("""
                UPDATE "Setor"
                SET "Codigo" = upper(replace(trim("Sigla"), ' ', '_'))
                WHERE "Codigo" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                schema: "public",
                table: "Setor",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Nucleo",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ChefeServidorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nucleo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nucleo_Servidor_ChefeServidorId",
                        column: x => x.ChefeServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Setor_NucleoId",
                schema: "public",
                table: "Setor",
                column: "NucleoId");

            migrationBuilder.CreateIndex(
                name: "IX_Nucleo_ChefeServidorId",
                schema: "public",
                table: "Nucleo",
                column: "ChefeServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_Nucleo_Nome",
                schema: "public",
                table: "Nucleo",
                column: "Nome");

            migrationBuilder.AddForeignKey(
                name: "FK_Setor_Nucleo_NucleoId",
                schema: "public",
                table: "Setor",
                column: "NucleoId",
                principalSchema: "public",
                principalTable: "Nucleo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Setor_Nucleo_NucleoId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropForeignKey(
                name: "FK_Setor_Servidor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropTable(
                name: "Nucleo",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Setor_ChefeSubstitutoServidorId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropIndex(
                name: "IX_Setor_Codigo",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropIndex(
                name: "IX_Setor_NucleoId",
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

            migrationBuilder.DropColumn(
                name: "NucleoId",
                schema: "public",
                table: "Setor");

            migrationBuilder.DropColumn(
                name: "Resumo",
                schema: "public",
                table: "Setor");
        }
    }
}
