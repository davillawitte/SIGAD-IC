using Microsoft.EntityFrameworkCore;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Persistence.Seed;

public static class TipoOcorrenciaSeed
{
    private static readonly (string Codigo, string Nome, CategoriaOcorrencia Categoria, decimal? Horas)[] Catalog =
    [
        ("M", "Manhã 6h", CategoriaOcorrencia.Trabalho, 6m),
        ("T", "Tarde 6h", CategoriaOcorrencia.Trabalho, 6m),
        ("TL6", "Teletrabalho 6h", CategoriaOcorrencia.Trabalho, 6m),
        ("PD", "Plantão Diurno 12h", CategoriaOcorrencia.Trabalho, 12m),
        ("PN", "Plantão Noturno 12h", CategoriaOcorrencia.Trabalho, 12m),
        ("PT", "Plantão 24h", CategoriaOcorrencia.Trabalho, 24m),
        ("CF", "Chefia Núcleo 12h", CategoriaOcorrencia.Trabalho, 12m),
        ("FR", "Férias", CategoriaOcorrencia.Afastamento, null),
        ("F", "Feriado", CategoriaOcorrencia.Folga, null),
        ("LP", "Licença Prêmio", CategoriaOcorrencia.Afastamento, null),
        ("LM", "Licença Médica", CategoriaOcorrencia.Afastamento, null),
        ("LO", "Licença Outros", CategoriaOcorrencia.Afastamento, null),
        ("D", "Descanso", CategoriaOcorrencia.Folga, null),
        ("R", "Remoção", CategoriaOcorrencia.Outro, null),
    ];

    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var existing = await context.TiposOcorrencia.Select(x => x.Codigo).ToListAsync(cancellationToken);
        foreach (var (codigo, nome, categoria, horas) in Catalog)
        {
            if (existing.Contains(codigo))
            {
                continue;
            }

            context.TiposOcorrencia.Add(TipoOcorrencia.Create(codigo, nome, categoria, horas));
        }
    }
}
