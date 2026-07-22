using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Setor : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Sigla { get; private set; } = null!;
    public string? Resumo { get; private set; }
    public Guid? NucleoId { get; private set; }

    public Nucleo? Nucleo { get; private set; }
    public ICollection<SetorChefia> Chefias { get; private set; } = [];
    public ICollection<Servidor> Servidores { get; private set; } = [];

    private Setor()
    {
    }

    public static Setor Create(
        string nome,
        string sigla,
        Guid? nucleoId,
        string? resumo = null,
        string? createdBy = null,
        Guid? id = null)
    {
        var setor = new Setor
        {
            Nome = nome.Trim(),
            Sigla = NormalizeSigla(sigla),
            Resumo = string.IsNullOrWhiteSpace(resumo) ? null : resumo.Trim(),
            NucleoId = nucleoId,
        };

        if (id.HasValue)
        {
            setor.SetId(id.Value);
        }

        setor.MarkCreated(createdBy);
        return setor;
    }

    public void Atualizar(string nome, string sigla, string? resumo, Guid? nucleoId, string? updatedBy = null)
    {
        Nome = nome.Trim();
        Sigla = NormalizeSigla(sigla);
        Resumo = string.IsNullOrWhiteSpace(resumo) ? null : resumo.Trim();
        NucleoId = nucleoId;
        MarkUpdated(updatedBy);
    }

    public void AtualizarResumo(string? resumo, string? updatedBy = null)
    {
        Resumo = string.IsNullOrWhiteSpace(resumo) ? null : resumo.Trim();
        MarkUpdated(updatedBy);
    }

    public static string NormalizeSigla(string sigla) => sigla.Trim();
}
