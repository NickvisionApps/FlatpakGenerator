[![Nuget](https://img.shields.io/nuget/v/Nickvision.FlatpakGenerator)](https://www.nuget.org/packages/Nickvision.FlatpakGenerator/)

# Nickvision.FlatpakGenerator

<img width='128' height='128' alt='Logo' src='Nickvision.FlatpakGenerator/Resources/logo-r.png'/>

 **A tool to generate Flatpak sources file for a C# project**

 This tool is a replacement for [flatpak-dotnet-generator](https://github.com/flatpak/flatpak-builder-tools/tree/master/dotnet) python script, with some changes:
 1. Written in C# (obviously)
 2. Latest version of Freedesktop SDK and Dotnet are used
 3. Runtime packages required to build self-contained programs get added automatically (can be disabled with `--no-self-contained`)
 4. You can set additional packages to add with `-a` option

Example:

`flatpak-dotnet-generator YourProject.csproj -o sources.json -d dotnet-sources -a cake.tool cake.filehelpers`

Run `flatpak-dotnet-generator --help` to see full list of options.

# Installation
<a href='https://www.nuget.org/packages/Nickvision.FlatpakGenerator/'><img width='140' alt='Download on Nuget' src='https://www.nuget.org/Content/gallery/img/logo-header.svg'/></a>
