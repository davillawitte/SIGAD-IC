using System.Text.RegularExpressions;
using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class Servidor : BaseEntity
{
    private static readonly Regex MatriculaRegex = new(
        @"^\d{1,3}\.\d{3}-\d$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Nome { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public Guid CargoId { get; private set; }
    public string Email { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public DateOnly DataNascimento { get; private set; }
    public Guid? SetorId { get; private set; }
    public Guid? NucleoId { get; private set; }
    public StatusServidor Status { get; private set; } = StatusServidor.Ativo;

    public Cargo Cargo { get; private set; } = null!;
    public Setor? Setor { get; private set; }
    public Nucleo? Nucleo { get; private set; }
    public Usuario? Usuario { get; private set; }

    public bool EstaAtivo => Status == StatusServidor.Ativo;

    /// <summary>Servidor vinculado direto ao núcleo (chefe de núcleo ou lotado sem setor específico).</summary>
    public bool LotadoDiretoNoNucleo => NucleoId.HasValue;

    private Servidor()
    {
    }

    public static Servidor Create(
        string nome,
        string matricula,
        string cpf,
        Guid cargoId,
        string? email,
        Guid? setorId,
        Guid? nucleoId,
        DateOnly dataNascimento,
        string? telefone = null,
        StatusServidor status = StatusServidor.Ativo,
        string? createdBy = null,
        Guid? id = null)
    {
        ValidateLotacao(setorId, nucleoId);

        var servidor = new Servidor
        {
            Nome = nome.Trim(),
            Matricula = NormalizeMatricula(matricula),
            Cpf = NormalizeCpf(cpf),
            CargoId = cargoId,
            Email = NormalizeEmail(email),
            Telefone = NormalizeTelefone(telefone),
            DataNascimento = dataNascimento,
            SetorId = setorId,
            NucleoId = nucleoId,
            Status = status,
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
        Guid cargoId,
        string? email,
        Guid? setorId,
        Guid? nucleoId,
        DateOnly dataNascimento,
        string? telefone = null,
        string? updatedBy = null)
    {
        ValidateLotacao(setorId, nucleoId);

        Nome = nome.Trim();
        Matricula = NormalizeMatricula(matricula);
        Cpf = NormalizeCpf(cpf);
        CargoId = cargoId;
        Email = NormalizeEmail(email);
        Telefone = NormalizeTelefone(telefone);
        DataNascimento = dataNascimento;
        SetorId = setorId;
        NucleoId = nucleoId;
        MarkUpdated(updatedBy);
    }

    private static void ValidateLotacao(Guid? setorId, Guid? nucleoId)
    {
        if (setorId.HasValue == nucleoId.HasValue)
        {
            throw new ArgumentException(
                "Informe o setor de lotação ou o núcleo de lotação direta, nunca os dois nem nenhum.");
        }
    }

    public void DefinirStatus(StatusServidor status, string? updatedBy = null)
    {
        Status = status;
        MarkUpdated(updatedBy);
    }

    public static bool IsMatriculaValida(string? matricula) =>
        !string.IsNullOrWhiteSpace(matricula) && MatriculaRegex.IsMatch(matricula.Trim());

    public static string NormalizeMatricula(string matricula) => matricula.Trim();

    public static string NormalizeCpf(string cpf) =>
        new string(cpf.Where(char.IsDigit).ToArray());

    public static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    public static string? NormalizeTelefone(string? telefone) =>
        string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
}
