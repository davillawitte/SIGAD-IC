using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class TipoOcorrencia
{
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public decimal? HorasPadrao { get; private set; }
    public CategoriaOcorrencia Categoria { get; private set; }
    public bool Ativo { get; private set; } = true;

    private TipoOcorrencia()
    {
    }

    public static TipoOcorrencia Create(
        string codigo,
        string nome,
        CategoriaOcorrencia categoria,
        decimal? horasPadrao = null,
        bool ativo = true) =>
        new()
        {
            Codigo = codigo.Trim().ToUpperInvariant(),
            Nome = nome.Trim(),
            Categoria = categoria,
            HorasPadrao = horasPadrao,
            Ativo = ativo,
        };
}
