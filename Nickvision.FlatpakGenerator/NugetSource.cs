using System.Text.Json.Serialization;

namespace Nickvision.FlatpakGenerator;

public class NugetSource
{
    [JsonPropertyName("dest")]
    public required string Destination { get; set; }
    [JsonPropertyName("dest-filename")]
    public required string DestinationFileName { get; set; }
    public required string Sha512 { get; set; }
    public required string Url { get; set; }

    public string Type => "file";
}
