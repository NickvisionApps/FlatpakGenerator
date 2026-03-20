using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalogPage
{
    public int Count { get; set; }
    [JsonPropertyName("items")]
    public List<NugetCatalogPackage> Packages { get; set; }
    [JsonPropertyName("@id")]
    public string Url { get; set; }
    [JsonPropertyName("catalogEntry")]
    public string? CatalogEntryUrl { get; set; }

    public NugetCatalogPage()
    {
        Url = string.Empty;
        Count = 0;
        Packages = [];
        CatalogEntryUrl = null;
    }
}
