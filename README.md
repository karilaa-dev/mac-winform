# Mac WinForms

Build and run an existing C# Windows Forms project as a native macOS app with the stock .NET SDK.

## Install and run

Install the [.NET SDK for macOS](https://dotnet.microsoft.com/download) (8 or later), then install the tool once.

```sh
dotnet tool install --global MacWinForms.Tool --add-source https://raw.githubusercontent.com/karilaa-dev/mac-winform/main/feed/index.json
```

Run this in your WinForms project folder.

```sh
dotnet winforms
```

The tool builds your original project and opens its window. It includes the managed runtime and native AppKit bridge for Apple silicon and Intel. If your shell cannot find the command after installation, run `export PATH="$HOME/.dotnet/tools:$PATH"` and retry. Install the matching SDK and runtime for projects targeting .NET 9 or newer.

## Command examples

```sh
dotnet winforms ./MyApp/MyApp.csproj       # Run a specific project
dotnet winforms build ./MyApp -c Release  # Print the path to the native executable
dotnet winforms ./MyApp -- "two words"    # Forward arguments to the app
dotnet winforms --help
```

You can also pass a solution or select the original Windows target with `-f net8.0-windows`. Build output goes to `~/Library/Caches/mac-winform`. Set `MAC_WINFORM_CACHE` to choose another location. The executable uses the installed .NET runtime and the DLLs next to it.

### Using stock `dotnet run` and MSBuild

For a single-target SDK project, you can explicitly import the installed targets. Both commands below work with SDK 8 and 9.

```sh
targets="$(dotnet winforms --print-targets)"
BeforeTargetFrameworkInferenceTargets="$targets" dotnet run --project ./MyApp.csproj
dotnet msbuild ./MyApp.csproj /restore /t:Build,Run "/p:BeforeTargetFrameworkInferenceTargets=$targets"
```

With SDK 9, `dotnet run --project ./MyApp.csproj "-p:BeforeTargetFrameworkInferenceTargets=$targets"` also works. SDK 8 applies this command-line property at build time but looks in the original Windows output directory at launch. The environment-variable form applies to both stages.

The `/p:` switch sets an MSBuild property. Arguments after `--` go to the application. For solutions, classic projects, multi-target builds, or isolated output, use `dotnet winforms` to handle project discovery, target selection, and launch. [Microsoft .NET tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools#invoke-a-global-tool) · [dotnet run options](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)

## Compatibility

SDK-style C# projects targeting `net5.0-windows` or newer can build from their original project files. The tool targets .NET 8 for .NET 5–7 projects. It preserves the target major version for newer projects. Classic C# .NET Framework 4.x WinForms project files can also run through a generated stock-SDK project in the build cache. A project's `global.json` must select a compatible installed SDK.

All **53** concrete public WinForms control classes with a public parameterless constructor in .NET 8 can create native peers on the tested Mac. Individual controls have partial member and behavior coverage. The catalogue, interactive tests, public API inventory and known gaps are documented in the [compatibility report](docs/compatibility.md).

| Area | Examples of working behavior |
| --- | --- |
| Forms and layout | Modal windows, MDI activation and tiling, default buttons, AppKit message loop, scrolling, docking, tables, splitters and tool strips |
| Input controls | Secure, masked and rich text, dates and month calendars, numeric and domain spinners, checkable lists, link labels, sliders and scroll bars |
| Data and selection | Grid text, checkbox and combo cells, binding, validation, row addition/deletion, binding navigator, editable property grid and list views |
| Desktop integration | Format-aware clipboard, drag and drop, menus, file/font/color dialogs, tooltips, error providers and embedded WebKit browsing |
| Drawing and output | Custom painting, bitmap pixels, image codecs, text and theme rendering, designer images, print preview and multipage PDF output |

Win32/COM-dependent apps and apps relying on unimplemented framework members can still require changes. Forms use a left-aligned Windows-style title and right-hand caption buttons. The default light theme follows Windows focus, selection and control geometry for tabs, text fields, trees, lists, grids, sliders and checkable controls. Font rasterization, window corners, system panels and some widgets retain their macOS appearance.

## Record Library benchmark

The same source ran on Windows 11 with Microsoft's WinForms and on macOS with this layer. Its 750 × 450 client matched on all 12 recorded state values and all 10 control rectangles. Exact RGB matches across that client increased from 81.82% in 0.2.0 to 95.54% in 0.2.1. [Screenshots and benchmark instructions](tests/Gallery/README.md)

## Build from source

Building the tool itself on macOS requires the Xcode Command Line Tools (`xcode-select --install`). Run these commands from this checkout.

```sh
dotnet pack cli/MacWinForms.Tool.csproj -c Release -o dist
./bin/mac-winform tests/Gallery
```

Contributors can run `python3 tests/verify.py` to install the packaged tool in a temporary location and exercise the native fixtures.
