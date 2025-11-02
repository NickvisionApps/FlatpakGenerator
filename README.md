[![Nuget](https://img.shields.io/nuget/v/Nickvision.FlatpakGenerator)](https://www.nuget.org/packages/Nickvision.FlatpakGenerator/)

# Nickvision.FlatpakGenerator

<img width='128' height='128' alt='Logo' src='resources/logo-rounded.png'/>

**A tool to generate Flatpak sources file for .NET projectst**

This tool is a replacement for the [flatpak-dotnet-generator](https://github.com/flatpak/flatpak-builder-tools/tree/master/dotnet) python script, with some changes:
1. Written in C# (obviously)
2. Support for adding runtime packages that are required to build self-contained programs (enabled with `--self-contained`/`-s` flag)
3. Support for running `flatpak` commands in user mode (enabled with `--user`/`-u` flag)

## Dependencies
- .NET 9.0