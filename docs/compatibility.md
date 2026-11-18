# WinForms compatibility on macOS

The runtime compiles C# source against a Mac implementation of Windows Forms. The native app host runs on the installed Microsoft .NET runtime and calls AppKit, Core Graphics and WebKit. Project dependencies must also run on macOS. The .NET tool ships the managed compatibility assembly and a universal native library for Apple silicon and Intel. It uses the platform frameworks supplied by macOS and loads WebKit on first browser creation. Two build-only Microsoft analyzer assemblies generate the standard C# application bootstrap.

## What the tests measure

Version 0.3.0 passed all 17 native fixtures and both source-project integration tests on an Apple silicon Mac using the installed .NET tool. Intel is included in the native binary. The interactive tests ran on Apple silicon.

| Test | Evidence |
| --- | --- |
| Stock controls | All 53 concrete public control types with a public parameterless constructor in the .NET 8 reference can be instantiated, given a native peer and disposed on this Mac. This includes controls commonly hosted by other controls such as the two DataGridView editors. [Catalogue](../tests/ControlCatalog/Program.cs) |
| Windows comparison | The unchanged Record Library gallery ran on Windows 11 and macOS. Its client was 750 × 450 on both. Twelve application-state values and ten client-relative rectangles matched. [Gallery, screenshots and reproduction](../tests/Gallery/README.md) |
| Input and data | AppKit tests cover text and combo grid editors, validation, row creation and deletion, list currency, binding navigation, rich text and masks, tree/list selection, splitter movement, keyboard shortcuts, focus, clipboard and drag-and-drop. [Grid](../tests/Grid/Program.cs), [widgets](../tests/Widgets/Program.cs), [input](../tests/Interaction/Program.cs) |
| Designer resources | A stock ComponentResourceManager loads bitmaps, icons, image lists and localized strings using the original .resx files. [Resources](../tests/Resources/Program.cs) |
| Drawing and printing | Tests read actual pixels after bitmap, image, text and theme drawing. The printing fixture renders two pages with different orientations, exercises print preview and writes a PDF. Physical printer output requires separate testing. [Drawing](../tests/Drawing/Program.cs), [theme](../tests/Theme/Program.cs), [printing](../tests/Printing/Program.cs) |
| Forms and components | Tests cover native caption button actions, initial tab focus, preserved client dimensions, modal dialogs, tab and MDI child windows, cancellation, offline WebKit navigation, scrolling, editable nested properties, tooltips, error providers, color and font selection and menu-bar icon events. The layout fixture also passed on Windows 11 using the stock Windows SelectTab path. [Window captions](../tests/Caption/Program.cs), [layout](../tests/Layout/Program.cs), [menu-bar icon](../tests/NotifyIcon/Program.cs), [Desktop](../tests/Desktop/Program.cs), [property grid](../tests/PropertyGrid/Program.cs), [providers](../tests/Providers/Program.cs) |

## Public API inventory

The 0.2.1 release matches **742 of 1,018 public type names** and **5,127 of 13,998 declared signatures**. A type-name match can have missing members. The 53-control construction test above covers a smaller, independently tested set.

The [compressed JSON report](winforms-net8-api.json.gz) lists the missing signatures for each type.

The reference is Microsoft.WindowsDesktop.App.Ref 8.0.20, using System.Windows.Forms.dll and System.Windows.Forms.Primitives.dll. The tool lists visible types in System.Windows.Forms and its subnamespaces, including nested types. It compares methods, constructors, property/event accessors, and public or protected fields declared on each type. Enum names and values and predefined visual-style descriptors count toward these totals. Assembly identity, type inheritance, default parameters, interaction behavior, theme fidelity and designer services require separate tests. Inherited members are counted at their declaring type. System.Drawing is outside this inventory.

Generate the current totals and a JSON report of missing signatures from the repository root. Set REFPACK to the installed copy of the reference pack.

```sh
REFPACK="$HOME/.nuget/packages/microsoft.windowsdesktop.app.ref/8.0.20/ref/net8.0"
dotnet build runtime -c Release --artifacts-path /tmp/macforms-inventory
dotnet run --project tools/ApiInventory -- "$REFPACK" \
  /tmp/macforms-inventory/bin/MacWinForms/release/MacWinForms.dll /tmp/macforms-inventory/report.json
```

The renderer supplies the predefined VisualStyleElement descriptors and draws selected parts of BUTTON, EDIT, TAB, COMBOBOX, PROGRESS and HEADER. Use VisualStyleRenderer.IsElementDefined to query supported parts. [Microsoft reference source](https://github.com/dotnet/winforms/tree/v8.0.20)

## Current boundaries

- HWNDs, direct calls to native Windows DLLs, ActiveX controls, Office COM automation, OLE embedding, Windows UI Automation providers and Windows-only third-party binaries need a platform-specific implementation. Mac handles represent local peers.
- Advanced DataGridView virtual mode, custom editing controls, specialized property editors, and portions of System.Drawing/GDI+ remain incomplete.
- WebBrowser uses WKWebView. Applications depending on Internet Explorer's ActiveX object model, COM DOM, or proprietary rendering behavior require changes.
- Classic projects use evaluated sources, resources, package and project references. Windows-only dependencies, custom generators, build events and targets can require individual adaptation. Resource conversion supports known bitmap, icon and image-list encodings. Arbitrary serialized object graphs are unsupported.
- The Windows Forms visual designer, design-time services, and pixel-for-pixel rendering across all themes/DPI settings remain outside the current implementation. AppKit supplies window management, font rasterization, input methods, system panels and parts of control drawing. The shared renderer draws the form caption and common Windows light-theme controls.
