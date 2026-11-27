using Microsoft.Extensions.Hosting;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>Imagens usadas no cabeçalho dos PDFs (brasões), procuradas nos mesmos lugares em
/// desenvolvimento e no container: wwwroot da API ou ao lado do executável.</summary>
public static class PdfAssets
{
    public const string BrasaoPci = "logo-pci-govrn-footer.png";
    public const string BrasaoRn = "brasao-rn.png";

    public static byte[]? LoadImage(IHostEnvironment env, string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(env.ContentRootPath, "wwwroot", "assets", "images", fileName),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "images", fileName),
            Path.Combine(AppContext.BaseDirectory, "assets", "images", fileName),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
        }

        return null;
    }
}
