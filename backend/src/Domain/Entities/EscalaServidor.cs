using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

public class EscalaServidor : BaseEntity
{
    public Guid EscalaId { get; private set; }
    public Guid ServidorId { get; private set; }
    public Guid CargoId { get; private set; }
    public int Ordem { get; private set; }
    public string ServidorNome { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public string CargoNome { get; private set; } = null!;
    public string CargoCodigo { get; private set; } = null!;

    public Escala Escala { get; private set; } = null!;
    public Servidor Servidor { get; private set; } = null!;
    public Cargo Cargo { get; private set; } = null!;
    public ICollection<EscalaJornada> Jornadas { get; private set; } = [];
    public ICollection<EscalaOcorrencia> Ocorrencias { get; private set; } = [];

    private EscalaServidor()
    {
    }

    public static EscalaServidor Create(
        Guid escalaId,
        Guid servidorId,
        Guid cargoId,
        int ordem,
        string servidorNome,
        string matricula,
        string cargoNome,
        string cargoCodigo,
        string? createdBy = null)
    {
        var item = new EscalaServidor
        {
            EscalaId = escalaId,
            ServidorId = servidorId,
            CargoId = cargoId,
            Ordem = ordem,
            ServidorNome = servidorNome.Trim(),
            Matricula = matricula.Trim(),
            CargoNome = cargoNome.Trim(),
            CargoCodigo = cargoCodigo.Trim(),
        };

        item.MarkCreated(createdBy);
        return item;
    }

    public void AtualizarOrdem(int ordem, string? updatedBy = null)
    {
        Ordem = ordem;
        MarkUpdated(updatedBy);
    }

    public void AtualizarSnapshot(
        Guid cargoId,
        string servidorNome,
        string matricula,
        string cargoNome,
        string cargoCodigo,
        string? updatedBy = null)
    {
        CargoId = cargoId;
        ServidorNome = servidorNome.Trim();
        Matricula = matricula.Trim();
        CargoNome = cargoNome.Trim();
        CargoCodigo = cargoCodigo.Trim();
        MarkUpdated(updatedBy);
    }
}
