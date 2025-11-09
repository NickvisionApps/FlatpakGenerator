using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalogPackage
{
    public NugetCatalogEntry CatalogEntry { get; set; }
    [JsonPropertyName("@id")]
    public string Url { get; set; }

    public NugetCatalogPackage()
    {
        Url = string.Empty;
        CatalogEntry = new NugetCatalogEntry();
    }
}
