using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nickvision.FlatpakGenerator;

public class FlatpakSourcesGenerator
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonSerializerOptions;

    static FlatpakSourcesGenerator()
    {
        HttpClient = new HttpClient();
        JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public static async Task<bool> CheckRuntimeAsync(string runtime, bool runAsUser)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "flatpak",
                ArgumentList =
                {
                    "list",
                    "--runtime",
                    "--columns=application,branch"
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        if (runAsUser)
        {
            process.StartInfo.ArgumentList.Insert(1, "--user");
        }
        process.Start();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            Console.WriteLine($"[Missing] {runtime} (Install with 'flatpak install {runtime}')");
            return false;
        }
        var output = await process.StandardOutput.ReadToEndAsync();
        foreach (var line in output.Split('\n'))
        {
            if (line.Split('\t').SequenceEqual(runtime.Split("//")))
            {
                Console.WriteLine($"[Found] {runtime}");
                return true;
            }
        }
        Console.WriteLine($"[Missing] {runtime} (Install with 'flatpak install {runtime}')");
        return false;
    }

    public static async Task<List<NugetSource>> GenerateSourcesAsync(string input, int dotnetVersion, string? temp, bool selfContained, bool runAsUser)
    {
        input = input.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        temp = Path.Combine(temp?.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) ?? Directory.GetCurrentDirectory(), "nuget-temp");
        Directory.CreateDirectory(temp);
        if (string.IsNullOrEmpty(input) || !File.Exists(input) || Path.GetExtension(input) != ".csproj")
        {
            Console.Error.WriteLine("[Error] Invalid input csproj file path");
            return [];
        }
        if (!await CheckRuntimeAsync($"org.freedesktop.Sdk//24.08", runAsUser))
        {
            return [];
        }
        if (!await CheckRuntimeAsync($"org.freedesktop.Sdk.Extension.dotnet{dotnetVersion}//24.08", runAsUser))
        {
            return [];
        }
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "flatpak",
                ArgumentList =
                {
                    "run",
                    "--env=DOTNET_CLI_TELEMETRY_OPTOUT=true",
                    "--env=DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true",
                    "--command=sh",
                    "--runtime=org.freedesktop.Sdk//24.08",
                    "--share=network",
                    "--filesystem=host",
                    $"org.freedesktop.Sdk.Extension.dotnet{dotnetVersion}//24.08",
                    "-c",
                    $"PATH=\"${{PATH}}:/usr/lib/sdk/dotnet{dotnetVersion}/bin\" LD_LIBRARY_PATH=\"$LD_LIBRARY_PATH:/usr/lib/sdk/dotnet{dotnetVersion}/lib\" exec dotnet restore \"$@\"",
                    "--",
                    "--packages",
                    temp,
                    input
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        if (runAsUser)
        {
            process.StartInfo.ArgumentList.Insert(1, "--user");
        }
        process.Start();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"[Error] Unable to restore project: {await process.StandardOutput.ReadToEndAsync()}");
            Directory.Delete(temp, true);
            return [];
        }
        var sources = new List<NugetSource>();
        foreach (var file in Directory.GetFiles(temp, "*.nupkg.sha512", SearchOption.AllDirectories))
        {
            using var reader = new StreamReader(file);
            var hash = Convert.ToHexString(Convert.FromBase64String(await reader.ReadToEndAsync())).ToLower();
            var fields = file.Split('/');
            var name = fields[^3];
            var version = fields[^2];
            var filename = $"{name}.{version}.nupkg";
            Console.WriteLine($"[Found] {name}");
            sources.Add(new NugetSource
            {
                Url = $"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}",
                Sha512 = hash,
                Destination = "nuget-sources",
                DestinationFileName = filename
            });
        }
        Directory.Delete(temp, true);
        if (selfContained)
        {
            foreach (var extra in new[]
            {
                "microsoft.aspnetcore.app.runtime.linux-arm",
                "microsoft.aspnetcore.app.runtime.linux-arm64",
                "microsoft.aspnetcore.app.runtime.linux-x64",
                "microsoft.netcore.app.runtime.linux-arm",
                "microsoft.netcore.app.runtime.linux-arm64",
                "microsoft.netcore.app.runtime.linux-x64"
            })
            {
                var extraSource = await GetExtraSourceAsync(extra);
                if (extraSource is not null)
                {
                    sources.Add(extraSource);
                }
            }
        }
        sources.Sort((s1, s2) => s1.DestinationFileName.CompareTo(s2.DestinationFileName));
        Console.WriteLine($"[Info] Generated {sources.Count} source{(sources.Count == 1 ? "" : "s")}");
        return sources;
    }

    public static async Task WriteSourcesFileAsync(List<NugetSource> sources, string? output)
    {
        output = output?.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (string.IsNullOrEmpty(output))
        {
            output = "nuget-sources.json";
        }
        else if (!string.IsNullOrEmpty(output) && Path.GetExtension(output) != ".json")
        {
            output += ".json";
        }
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(sources, JsonSerializerOptions));
        Console.WriteLine($"[Info] Sources file written to {Path.GetFullPath(output)}");
    }

    private static async Task<NugetSource?> GetExtraSourceAsync(string name)
    {
        name = name.ToLower();
        var catalog = await HttpClient.GetFromJsonAsync<NugetCatalog>($"https://api.nuget.org/v3/registration5-semver1/{name}/index.json", JsonSerializerOptions);
        if (catalog is null || catalog.Count == 0)
        {
            Console.Error.WriteLine($"[Error] Unable to find package: {name}");
            return null;
        }
        var latestPage = catalog.Pages[^1];
        latestPage = await HttpClient.GetFromJsonAsync<NugetCatalogPage>(latestPage.Url, JsonSerializerOptions);
        if (latestPage is null || latestPage.Count == 0)
        {
            Console.Error.WriteLine($"[Error] Unable to find package: {name}");
            return null;
        }
        var latestEntry = latestPage.Packages[^1].CatalogEntry;
        latestEntry = await HttpClient.GetFromJsonAsync<NugetCatalogEntry>(latestEntry.Url, JsonSerializerOptions);
        if (latestEntry is null)
        {
            Console.Error.WriteLine($"[Error] Unable to find package: {name}");
            return null;
        }
        var filename = $"{name}.{latestEntry.Version}.nupkg";
        Console.WriteLine($"[Found] {name}");
        return new NugetSource
        {
            Url = $"https://api.nuget.org/v3-flatcontainer/{name}/{latestEntry.Version}/{filename}",
            Sha512 = Convert.ToHexString(Convert.FromBase64String(latestEntry.PackageHash)).ToLower(),
            Destination = "nuget-sources",
            DestinationFileName = filename
        };
    }
}
