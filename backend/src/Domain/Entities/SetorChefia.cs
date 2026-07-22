using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Domain.Entities;

public class SetorChefia
{
    public Guid SetorId { get; private set; }
    public Guid ServidorId { get; private set; }
    public TipoChefia TipoChefia { get; private set; }

    public Setor Setor { get; private set; } = null!;
    public Servidor Servidor { get; private set; } = null!;

    private SetorChefia()
    {
    }

    public static SetorChefia Create(Guid setorId, Guid servidorId, TipoChefia tipoChefia) =>
        new()
        {
            SetorId = setorId,
            ServidorId = servidorId,
            TipoChefia = tipoChefia,
        };

    public void TrocarServidor(Guid servidorId) => ServidorId = servidorId;
}
