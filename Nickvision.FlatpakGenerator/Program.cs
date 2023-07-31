using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Nickvision.FlatpakGenerator;

public class Program
{
    public static async Task Main(string[] args) => await new Program().RunAsync(args);
    
    /// <summary>
    /// Construct Program
    /// </summary>
    private Program()
    {
    }

    /// <summary>
    /// Run the program
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    private async Task RunAsync(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async o =>
            {
                var sources = GenerateSourcesFromProject(o.InputFile, o.DestDir, o.TempDir);
                var addPackages = o.AdditionalPackages.ToList();
                if (o.SelfContained == true)
                {
                    addPackages.Add("microsoft.aspnetcore.app.runtime.linux-arm");
                    addPackages.Add("microsoft.aspnetcore.app.runtime.linux-arm64");
                    addPackages.Add("microsoft.aspnetcore.app.runtime.linux-x64");
                    addPackages.Add("microsoft.netcore.app.runtime.linux-arm");
                    addPackages.Add("microsoft.netcore.app.runtime.linux-arm64");
                    addPackages.Add("microsoft.netcore.app.runtime.linux-x64");
                }
                foreach (var pkg in addPackages)
                {
                    sources.Add(await GetPackageAsync(pkg, o.DestDir));
                }
                await File.WriteAllTextAsync(o.OutputFile, JsonSerializer.Serialize(sources, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"Sources are written to file \"{o.OutputFile}\"");
            });
    }
    
    /// <summary>
    /// Generate sources list from CSPROJ file
    /// </summary>
    /// <param name="inputFile">CSPROJ file</param>
    /// <param name="destDir">Destination directory for sources</param>
    /// <param name="tempDir">Temporary directory to restore packages to get data</param>
    /// <returns>List with packages data</returns>
    private List<Dictionary<string, string>> GenerateSourcesFromProject(string inputFile, string destDir, string tempDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "flatpak",
                ArgumentList = { "run",
                        "--env=DOTNET_CLI_TELEMETRY_OPTOUT=true", "--env=DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true",
                        "--command=sh", "--runtime=org.freedesktop.Sdk//22.08", "--share=network", "--filesystem=host", "org.freedesktop.Sdk.Extension.dotnet7//22.08", "-c",
                        "PATH=\"${PATH}:/usr/lib/sdk/dotnet7/bin\" LD_LIBRARY_PATH=\"$LD_LIBRARY_PATH:/usr/lib/sdk/dotnet7/lib\" exec dotnet restore \"$@\"",
                        "--", "--packages", tempDir, inputFile
                },
                UseShellExecute = false
            }
        };
        process.Start();
        process.WaitForExit();
        var result = new List<Dictionary<string, string>>();
        foreach (var file in Directory.GetFiles(tempDir, "*.nupkg.sha512", SearchOption.AllDirectories))
        {
            var split = file.Split("/");
            var name = split[^3];
            var version = split[^2];
            var filename = $"{name}.{version}.nupkg";
            using var reader = new StreamReader(file);
            var sha512 = Convert.ToHexString(Convert.FromBase64String(reader.ReadToEnd())).ToLower();
            result.Add(new Dictionary<string, string>
            {
                { "type", "file" },
                { "url", $"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}" },
                { "sha512", sha512 },
                { "dest", destDir },
                { "dest-filename", filename }
            });
        }
        Directory.Delete(tempDir, true);
        return result;
    }
    
    /// <summary>
    /// Get data for the latest version of the package
    /// </summary>
    /// <param name="name">Package name</param>
    /// <param name="destDir">Destination directory for sources</param>
    /// <returns>Data for the package</returns>
    private async Task<Dictionary<string, string>> GetPackageAsync(string name, string destDir)
    {
        name = name.ToLower();
        using var httpClient = new HttpClient();
        var regResponse = await httpClient.GetAsync($"https://api.nuget.org/v3/registration5-semver1/{name}/index.json");
        var regObj = JsonSerializer.Deserialize<JsonObject>(await regResponse.Content.ReadAsStringAsync())!;
        var catalogUrl = ((regObj["items"] as JsonArray)![^1]!["items"] as JsonArray)![^1]!["catalogEntry"]!["@id"]!.ToString();
        var catResponse = await httpClient.GetAsync(catalogUrl);
        var catObj = JsonSerializer.Deserialize<JsonObject>(await catResponse.Content.ReadAsStringAsync())!;
        var version = catObj["version"]!.ToString();
        var sha512 = Convert.ToHexString(Convert.FromBase64String(catObj["packageHash"]!.ToString())).ToLower();
        var filename = $"{name}.{version}.nupkg";
        Console.WriteLine($"Added package {filename}");
        return new Dictionary<string, string>
        {
            { "type", "file" },
            { "url", $"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}" },
            { "sha512", sha512 },
            { "dest", destDir },
            { "dest-filename", filename }
        };
    }
}