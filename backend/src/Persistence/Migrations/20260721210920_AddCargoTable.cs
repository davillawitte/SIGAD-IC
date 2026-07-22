using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoTable : Migration
    {
        private static readonly Guid PeritoCriminalId = Guid.Parse("174cf381-fb97-4301-aa54-227cbc179b23");
        private static readonly Guid AgenteTecnicoForenseId = Guid.Parse("9b80a4b6-4a6c-43c1-a08c-a30e1814644b");
        private static readonly Guid AgenteNecropsiaId = Guid.Parse("df9ad520-3051-4d7e-9f54-d43f7c13fc3e");
        private static readonly Guid AssistenteTecnicoForenseId = Guid.Parse("76470c0b-7c46-456f-8c10-053e75a92358");
        private static readonly Guid EstagiarioId = Guid.Parse("61b2394f-bdb8-4344-888c-e78e01a7f5e6");
        private static readonly Guid TerceirizadoId = Guid.Parse("86ebd46f-50f1-483b-b8ee-e84ae7d8205a");
        private static readonly Guid ServidorExternoId = Guid.Parse("661d799f-4307-463e-9215-dd84698c5d98");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cargo",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_Codigo",
                schema: "public",
                table: "Cargo",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_Nome",
                schema: "public",
                table: "Cargo",
                column: "Nome");

            var now = DateTime.UtcNow;
            InsertCargo(migrationBuilder, PeritoCriminalId, "Perito Criminal", "PERITO_CRIMINAL", now);
            InsertCargo(migrationBuilder, AgenteTecnicoForenseId, "Agente Técnico Forense", "AGENTE_TECNICO_FORENSE", now);
            InsertCargo(migrationBuilder, AgenteNecropsiaId, "Agente de Necrópsia", "AGENTE_NECROPSIA", now);
            InsertCargo(migrationBuilder, AssistenteTecnicoForenseId, "Assistente Técnico Forense", "ASSISTENTE_TECNICO_FORENSE", now);
            InsertCargo(migrationBuilder, EstagiarioId, "Estagiário", "ESTAGIARIO", now);
            InsertCargo(migrationBuilder, TerceirizadoId, "Terceirizado", "TERCEIRIZADO", now);
            InsertCargo(migrationBuilder, ServidorExternoId, "Servidor Externo", "SERVIDOR_EXTERNO", now);

            migrationBuilder.AddColumn<Guid>(
                name: "CargoId",
                schema: "public",
                table: "Servidor",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Servidor" s
                SET "CargoId" = c."Id"
                FROM "Cargo" c
                WHERE lower(trim(s."Cargo")) = lower(trim(c."Nome"));
                """);

            // Seed legado: Vitor / texto "Super Administrador" → Perito Criminal
            migrationBuilder.Sql($"""
                UPDATE "Servidor"
                SET "CargoId" = '{PeritoCriminalId}'
                WHERE "CargoId" IS NULL
                  AND (
                    "Id" = '22222222-2222-2222-2222-222222222222'
                    OR lower(trim(coalesce("Cargo", ''))) IN ('super administrador', 'super-administrador')
                  );
                """);

            // Evita falha 23502 ao tornar CargoId NOT NULL (qualquer residual sem match).
            migrationBuilder.Sql($"""
                UPDATE "Servidor"
                SET "CargoId" = '{PeritoCriminalId}'
                WHERE "CargoId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CargoId",
                schema: "public",
                table: "Servidor",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Cargo",
                schema: "public",
                table: "Servidor");

            migrationBuilder.CreateIndex(
                name: "IX_Servidor_CargoId",
                schema: "public",
                table: "Servidor",
                column: "CargoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servidor_Cargo_CargoId",
                schema: "public",
                table: "Servidor",
                column: "CargoId",
                principalSchema: "public",
                principalTable: "Cargo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servidor_Cargo_CargoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropIndex(
                name: "IX_Servidor_CargoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                schema: "public",
                table: "Servidor",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Servidor" s
                SET "Cargo" = c."Nome"
                FROM "Cargo" c
                WHERE s."CargoId" = c."Id";
                """);

            migrationBuilder.DropColumn(
                name: "CargoId",
                schema: "public",
                table: "Servidor");

            migrationBuilder.DropTable(
                name: "Cargo",
                schema: "public");
        }

        private static void InsertCargo(
            MigrationBuilder migrationBuilder,
            Guid id,
            string nome,
            string codigo,
            DateTime createdAt)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "Cargo",
                columns: ["Id", "Nome", "Codigo", "Ativo", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"],
                values: [id, nome, codigo, true, createdAt, null, "migration", null]);
        }
    }
}
