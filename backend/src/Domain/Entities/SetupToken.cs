using TemplateSistema.Domain.Common;

namespace TemplateSistema.Domain.Entities;

/// <summary>
/// Token de uso único do wizard de provisionamento (<c>/setup</c>). Só o hash é persistido —
/// o valor bruto é mostrado uma vez (log do operador) e nunca chega ao banco.
/// </summary>
public class SetupToken : BaseEntity
{
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private SetupToken()
    {
    }

    public static SetupToken Create(string tokenHash, TimeSpan ttl, string? createdBy = null)
    {
        var entity = new SetupToken
        {
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
        };

        entity.MarkCreated(createdBy);
        return entity;
    }

    public bool EstaValido(DateTime nowUtc) => UsedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void MarcarUsado()
    {
        UsedAtUtc = DateTime.UtcNow;
        MarkUpdated("setup");
    }
}
