using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemplateSistema.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermutaRecebida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermutaRecebida",
                schema: "public",
                table: "EscalaOcorrencia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Permutas já gravadas: o "*" passa a ficar só com quem recebeu o dia — o permutante
            // na data do solicitante e o solicitante na data do permutante (da última permuta).
            migrationBuilder.Sql("""
                UPDATE public."EscalaOcorrencia" o
                SET "PermutaRecebida" = TRUE
                FROM public."EscalaServidor" es, public."Permuta" p
                WHERE o."EscalaServidorId" = es."Id"
                  AND o."Permutada"
                  AND o."PermutaId" = p."Id"
                  AND ((o."Data" = p."DataIda" AND es."ServidorId" = p."PermutanteServidorId")
                    OR (o."Data" = p."DataVolta" AND es."ServidorId" = p."SolicitanteServidorId"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermutaRecebida",
                schema: "public",
                table: "EscalaOcorrencia");
        }
    }
}
