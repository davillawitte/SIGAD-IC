using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Servidores;

/// <summary>Listagem — sem CPF (PII), evita vazar em qualquer tela que só precisa listar.</summary>
public record ServidorListItemDto(
    Guid Id,
    string Nome,
    string Matricula,
    Guid CargoId,
    string Cargo,
    string CargoCodigo,
    string? Email,
    string? Telefone,
    DateOnly DataNascimento,
    Guid? SetorId,
    string? SetorNome,
    Guid? NucleoId,
    string? NucleoNome,
    bool PossuiUsuario,
    bool UsuarioAtivo,
    StatusServidor Status);

/// <summary>Item único (detalhe/criação/edição) — com CPF, usado só quando o operador já
/// está olhando um servidor específico (tela de edição), não em listagens.</summary>
public record ServidorDetalheDto(
    Guid Id,
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string Cargo,
    string CargoCodigo,
    string? Email,
    string? Telefone,
    DateOnly DataNascimento,
    Guid? SetorId,
    string? SetorNome,
    Guid? NucleoId,
    string? NucleoNome,
    bool PossuiUsuario,
    bool UsuarioAtivo,
    StatusServidor Status);

/// <summary>Informe SetorId (lotação num setor) ou NucleoId (lotação direta no núcleo — chefe de
/// núcleo/servidor que atua em todos os setores do núcleo), nunca os dois nem nenhum.</summary>
public record CreateServidorRequest(
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string? Email,
    Guid? SetorId,
    Guid? NucleoId,
    DateOnly DataNascimento,
    string? Telefone,
    StatusServidor? Status);

public record UpdateServidorRequest(
    string Nome,
    string Matricula,
    string Cpf,
    Guid CargoId,
    string? Email,
    Guid? SetorId,
    Guid? NucleoId,
    DateOnly DataNascimento,
    string? Telefone,
    StatusServidor Status);

public record ServidorExclusaoImpactoDto(
    int Escalas,
    int Afastamentos,
    int Chefias,
    int Usuarios,
    int NucleosComoChefe,
    bool PodeExcluir);
