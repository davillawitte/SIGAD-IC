using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class Servidor : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Cargo { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public Guid SetorId { get; private set; }
    public bool Ativo { get; private set; } = true;

    public Setor Setor { get; private set; } = null!;
    public Usuario? Usuario { get; private set; }

    private Servidor()
    {
    }

    public static Servidor Create(
        string nome,
        string matricula,
        string cpf,
        string cargo,
        string email,
        Guid setorId,
        string? telefone = null,
        string? createdBy = null,
        Guid? id = null)
    {
        var servidor = new Servidor
        {
            Nome = nome.Trim(),
            Matricula = matricula.Trim(),
            Cpf = NormalizeCpf(cpf),
            Cargo = cargo.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            SetorId = setorId,
            Ativo = true,
        };

        if (id.HasValue)
        {
            servidor.SetId(id.Value);
        }

        servidor.MarkCreated(createdBy);
        return servidor;
    }

    public void Atualizar(
        string nome,
        string matricula,
        string cpf,
        string cargo,
        string email,
        Guid setorId,
        string? telefone = null,
        string? updatedBy = null)
    {
        Nome = nome.Trim();
        Matricula = matricula.Trim();
        Cpf = NormalizeCpf(cpf);
        Cargo = cargo.Trim();
        Email = email.Trim().ToLowerInvariant();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        SetorId = setorId;
        MarkUpdated(updatedBy);
    }

    public void Ativar(string? updatedBy = null)
    {
        Ativo = true;
        MarkUpdated(updatedBy);
    }

    public void Desativar(string? updatedBy = null)
    {
        Ativo = false;
        MarkUpdated(updatedBy);
    }

    private static string NormalizeCpf(string cpf) =>
        new string(cpf.Where(char.IsDigit).ToArray());
}
