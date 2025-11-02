using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalogPackage
{
    [JsonPropertyName("@id")]
    public string Url { get; set; }
    public NugetCatalogEntry CatalogEntry { get; set; }
    
    public NugetCatalogPackage()
    {
        Url = string.Empty;
        CatalogEntry = new NugetCatalogEntry();
    }
}
