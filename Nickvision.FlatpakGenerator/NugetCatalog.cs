using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetCatalog
{
    [JsonPropertyName("@id")]
    public string Url { get; set; }
    public int Count { get; set; }
    [JsonPropertyName("items")]
    public List<NugetCatalogPage> Pages { get; set; }

    public NugetCatalog()
    {
        Url = string.Empty;
        Count = 0;
        Pages = [];
    }
}
