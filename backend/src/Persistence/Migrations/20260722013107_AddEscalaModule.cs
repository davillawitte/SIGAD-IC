using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Escala",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escala", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escala_Setor_SetorId",
                        column: x => x.SetorId,
                        principalSchema: "public",
                        principalTable: "Setor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TipoOcorrencia",
                schema: "public",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    HorasPadrao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoOcorrencia", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "EscalaServidor",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CargoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    ServidorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CargoNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CargoCodigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaServidor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaServidor_Cargo_CargoId",
                        column: x => x.CargoId,
                        principalSchema: "public",
                        principalTable: "Cargo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalaServidor_Escala_EscalaId",
                        column: x => x.EscalaId,
                        principalSchema: "public",
                        principalTable: "Escala",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaServidor_Servidor_ServidorId",
                        column: x => x.ServidorId,
                        principalSchema: "public",
                        principalTable: "Servidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalaJornada",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoJornada = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HoraFim = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Horas = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    TipoOcorrenciaCodigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecorrenciaTipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DiasSemana = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IntervaloDias = table.Column<int>(type: "integer", nullable: true),
                    DiasTrabalho = table.Column<int>(type: "integer", nullable: true),
                    DiasFolga = table.Column<int>(type: "integer", nullable: true),
                    TipoOcorrenciaFolgaCodigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaJornada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaJornada_EscalaServidor_EscalaServidorId",
                        column: x => x.EscalaServidorId,
                        principalSchema: "public",
                        principalTable: "EscalaServidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaJornada_TipoOcorrencia_TipoOcorrenciaCodigo",
                        column: x => x.TipoOcorrenciaCodigo,
                        principalSchema: "public",
                        principalTable: "TipoOcorrencia",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalaOcorrencia",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscalaServidorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoOcorrenciaCodigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HoraFim = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Horas = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EscalaJornadaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalaOcorrencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalaOcorrencia_EscalaJornada_EscalaJornadaId",
                        column: x => x.EscalaJornadaId,
                        principalSchema: "public",
                        principalTable: "EscalaJornada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EscalaOcorrencia_EscalaServidor_EscalaServidorId",
                        column: x => x.EscalaServidorId,
                        principalSchema: "public",
                        principalTable: "EscalaServidor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalaOcorrencia_TipoOcorrencia_TipoOcorrenciaCodigo",
                        column: x => x.TipoOcorrenciaCodigo,
                        principalSchema: "public",
                        principalTable: "TipoOcorrencia",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId",
                schema: "public",
                table: "Escala",
                column: "SetorId");

            migrationBuilder.CreateIndex(
                name: "IX_Escala_SetorId_DataInicio_DataFim",
                schema: "public",
                table: "Escala",
                columns: new[] { "SetorId", "DataInicio", "DataFim" });

            migrationBuilder.CreateIndex(
                name: "IX_Escala_Status",
                schema: "public",
                table: "Escala",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaJornada_EscalaServidorId",
                schema: "public",
                table: "EscalaJornada",
                column: "EscalaServidorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaJornada_TipoOcorrenciaCodigo",
                schema: "public",
                table: "EscalaJornada",
                column: "TipoOcorrenciaCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaOcorrencia_Data",
                schema: "public",
                table: "EscalaOcorrencia",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaOcorrencia_EscalaJornadaId",
                schema: "public",
                table: "EscalaOcorrencia",
                column: "EscalaJornadaId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaOcorrencia_EscalaServidorId_Data",
                schema: "public",
                table: "EscalaOcorrencia",
                columns: new[] { "EscalaServidorId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaOcorrencia_TipoOcorrenciaCodigo",
                schema: "public",
                table: "EscalaOcorrencia",
                column: "TipoOcorrenciaCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaServidor_CargoId",
                schema: "public",
                table: "EscalaServidor",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalaServidor_EscalaId_ServidorId",
                schema: "public",
                table: "EscalaServidor",
                columns: new[] { "EscalaId", "ServidorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalaServidor_ServidorId",
                schema: "public",
                table: "EscalaServidor",
                column: "ServidorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalaOcorrencia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaJornada",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EscalaServidor",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TipoOcorrencia",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Escala",
                schema: "public");
        }
    }
}
