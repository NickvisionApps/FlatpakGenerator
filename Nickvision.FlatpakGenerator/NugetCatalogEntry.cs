using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalogEntry
{
    public string PackageHash { get; set; }
    public string PackageHashAlgorithm { get; set; }
    [JsonPropertyName("@id")]
    public string Url { get; set; }
    public string Version { get; set; }

    public NugetCatalogEntry()
    {
        Url = string.Empty;
        PackageHashAlgorithm = string.Empty;
        PackageHash = string.Empty;
        Version = string.Empty;
    }
}
