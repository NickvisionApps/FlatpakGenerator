using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalogPage
{
    [JsonPropertyName("@id")]
    public string Url { get; set; }
    public int Count { get; set; }
    [JsonPropertyName("items")]
    public List<NugetCatalogPackage> Packages { get; set; }
    
    public NugetCatalogPage()
    {
        Url = string.Empty;
        Count = 0;
        Packages = [];
    }
}
