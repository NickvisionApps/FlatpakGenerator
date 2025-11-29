using System;
using System.CommandLine;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace Nickvision.FlatpakGenerator;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            Console.Error.WriteLine("This tool can only run on Linux.");
            return 1;
        }
        var rootCommand = new RootCommand("A tool to generate Flatpak sources file for .NET projects");
        var checkCommand = new Command("check", "Checks for the available of Flatpak .NET runtimes")
        {
            new Option<int>("--dotnet")
            {
                Description = "The .NET SDK version to target",
                Required = true,
                Aliases =
                {
                    "-d"
                },
                DefaultValueFactory = r => 10
            },
            new Option<string>("--freedesktop")
            {
                Description = "The FreeDesktop SDK version to target",
                Required = true,
                Aliases =
                {
                    "-f"
                },
                DefaultValueFactory = r => "25.08"
            },
            new Option<bool>("--run-as-user")
            {
                Description = "Whether to run flatpak commands as user",
                Required = false,
                Aliases =
                {
                    "-u"
                }
            }
        };
        var generateCommand = new Command("generate", "Generates a Flatpak sources file")
        {
            new Option<string>("--input")
            {
                Description = "The csproj file path",
                Required = true,
                Aliases =
                {
                    "-i"
                }
            },
            new Option<int>("--dotnet")
            {
                Description = "The .NET SDK version to target",
                Required = true,
                Aliases =
                {
                    "-d"
                },
                DefaultValueFactory = r => 10
            },
            new Option<string>("--freedesktop")
            {
                Description = "The FreeDesktop SDK version to target",
                Required = true,
                Aliases =
                {
                    "-f"
                },
                DefaultValueFactory = r => "25.08"
            },
            new Option<string>("--output")
            {
                Description = "The output Flatpak sources file path",
                Required = false,
                Aliases =
                {
                    "-o"
                }
            },
            new Option<bool>("--self-contained")
            {
                Description = "Whether to generate sources for a self-contained publish",
                Required = false,
                Aliases =
                {
                    "-s"
                }
            },
            new Option<bool>("--run-as-user")
            {
                Description = "Whether to run flatpak commands as user",
                Required = false,
                Aliases =
                {
                    "-u"
                }
            },
            new Option<string>("--temp")
            {
                Description = "The temporary directory path",
                Required = false,
                Aliases =
                {
                    "-t"
                }
            }
        };
        checkCommand.SetAction(async x =>
        {
            if(!IsDotnetVersionValid(x.GetValue<int>("--dotnet")))
            {
                Console.Error.WriteLine("[Error] Invalid .NET version. Supported versions are 8, 9, and 10.");
                return;
            }
            if(!IsFreedesktopVersionValid(x.GetValue<string>("--freedesktop"), x.GetValue<int>("--dotnet")))
            {
                Console.Error.WriteLine("[Error] Invalid FreeDesktop version for the specified .NET version.");
                return;
            }
            await FlatpakSourcesGenerator.CheckRuntimeAsync($"org.freedesktop.Sdk//{x.GetValue<string>("--freedesktop")}", x.GetValue<bool>("--run-as-user"));
            await FlatpakSourcesGenerator.CheckRuntimeAsync($"org.freedesktop.Sdk.Extension.dotnet{x.GetRequiredValue<int>("--dotnet")}//{x.GetValue<string>("--freedesktop")}", x.GetValue<bool>("--run-as-user"));
        });
        generateCommand.SetAction(async x =>
        {
            if (!IsDotnetVersionValid(x.GetValue<int>("--dotnet")))
            {
                Console.Error.WriteLine("[Error] Invalid .NET version. Supported versions are 8, 9, and 10.");
                return;
            }
            if (!IsFreedesktopVersionValid(x.GetValue<string>("--freedesktop"), x.GetValue<int>("--dotnet")))
            {
                Console.Error.WriteLine("[Error] Invalid FreeDesktop version for the specified .NET version.");
                return;
            }
            var sources = await FlatpakSourcesGenerator.GenerateSourcesAsync(x.GetRequiredValue<string>("--input"), x.GetRequiredValue<int>("--dotnet"), x.GetRequiredValue<string>("--freedesktop"), x.GetValue<string>("--temp"), x.GetValue<bool>("--self-contained"), x.GetValue<bool>("--run-as-user"));
            await FlatpakSourcesGenerator.WriteSourcesFileAsync(sources, x.GetValue<string>("--output"));
        });
        rootCommand.Subcommands.Add(checkCommand);
        rootCommand.Subcommands.Add(generateCommand);
        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static bool IsDotnetVersionValid(int dotnetVersion) => dotnetVersion >= 8 && dotnetVersion <= 10;

    private static bool IsFreedesktopVersionValid(string? freedesktopVersion, int dotnetVersion)
    {
        if(freedesktopVersion == "24.08")
        {
            return dotnetVersion != 8;
        }
        else if(freedesktopVersion == "25.08")
        {
            return true;
        }
        return false;
    }
}
