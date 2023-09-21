using CommandLine;
using System.Collections.Generic;

namespace Nickvision.FlatpakGenerator;

#pragma warning disable CS8618

/// <summary>
/// Command-line options
/// </summary>
internal class Options 
{
    /// <summary>
    /// Name of CSPROJ file to create sources for
    /// </summary>
    [Value(0, MetaName = "input", Required = true, HelpText = "CSPROJ file to generate sources for.")]
    public string InputFile { get; set; }
    /// <summary>
    /// Name of a file to write sources JSON data to
    /// </summary>
    [Option('o', "output", Required = false, Default = "nuget-sources.json", HelpText = "Output file name.")]
    public string OutputFile { get; set; }
    /// <summary>
    /// Destination directory set in output file where flatpak will download packages
    /// </summary>
    [Option('d', "dest-dir", Required = false, Default = "nuget-sources", HelpText = "Destination directory where flatpak will save sources to.")]
    public string DestDir { get; set; }
    /// <summary>
    /// Temporary directory where packages of CSPROJ will be restored to get data
    /// </summary>
    [Option('t', "temp-dir", Required = false, Default = "./FlatpakGeneratorPackages", HelpText = "Temporary directory to store packages (will be automatically removed after the process ends).")]
    public string TempDir { get; set; }
    /// <summary>
    /// Whether to NOT download runtime packages required to build self-contained apps
    /// </summary>
    [Option("no-self-contained", Required = false, HelpText = "Add runtime packages required to build self-contained apps.")]
    public bool NoSelfContained { get; set; }
    /// <summary>
    /// Whether to download runtime packages required to build self-contained apps
    /// </summary>
    [Option('u', "user", Required = false, HelpText = "Run flatpak in user mode.")]
    public bool RunAsUser { get; set; }
    /// <summary>
    /// Space-separated list of additional packages to download (only latest versions)
    /// </summary>
    [Option('a', "add-packages", Required = false, HelpText = "Additional packages list, latest versions will be downloaded automatically.")]
    public IEnumerable<string> AdditionalPackages { get; set; }
}

#pragma warning restore CS8618
