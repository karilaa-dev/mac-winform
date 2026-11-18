using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MacWinForms.Tool;

internal static class LegacyProjects
{
    private static readonly string[] ProjectProperties =
    [
        "OutputType", "AssemblyName", "RootNamespace", "StartupObject", "DefineConstants",
        "AllowUnsafeBlocks", "CheckForOverflowUnderflow", "LangVersion"
    ];

    private static string Value(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property.GetString() ?? "" : "";

    public static string Convert(string project, string configuration, string artifacts)
    {
        var converted = new Dictionary<string, string>();
        return ConvertProject(Path.GetFullPath(project));

        string ConvertProject(string path)
        {
            if (converted.TryGetValue(path, out var prior)) return prior;
            if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Classic project conversion requires C# source: {path}");

            var directory = Path.GetDirectoryName(path)!;
            var result = Launcher.ReadJson(Launcher.DotnetOutput(directory, "msbuild", path, "-nologo",
                "-p:Configuration=" + configuration,
                "-getProperty:UsingMicrosoftNETSdk,TargetFrameworkIdentifier," + string.Join(",", ProjectProperties),
                "-getItem:Compile,EmbeddedResource,Content,None,Reference,ProjectReference,PackageReference,COMReference"));
            var properties = result.GetProperty("Properties");
            var items = result.GetProperty("Items");
            bool framework = Value(properties, "TargetFrameworkIdentifier") == ".NETFramework";
            if (!framework && Value(properties, "UsingMicrosoftNETSdk").Equals("true", StringComparison.OrdinalIgnoreCase))
                return converted[path] = path;
            if (!framework) throw new InvalidDataException($"Cannot convert the project type of {path}.");
            if (items.GetProperty("COMReference").GetArrayLength() != 0)
                throw new InvalidDataException($"{path} requires Windows COM components.");

            var hash = System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..12].ToLowerInvariant();
            var target = Path.Combine(artifacts, "projects", hash, Path.GetFileName(path));
            converted[path] = target;

            var settings = new XElement("PropertyGroup",
                new XElement("TargetFramework", "net8.0-windows"), new XElement("UseWindowsForms", "true"),
                new XElement("EnableDefaultItems", "false"), new XElement("GenerateAssemblyInfo", "false"),
                new XElement("MacWinFormsClassic", "true"));
            foreach (var name in ProjectProperties)
                if (Value(properties, name) is { Length: > 0 } value) settings.Add(new XElement(name, value));

            var entries = new XElement("ItemGroup");
            bool needsSettings = false;
            foreach (var kind in new[] { "Compile", "EmbeddedResource", "Content", "None" })
            {
                foreach (var item in items.GetProperty(kind).EnumerateArray())
                {
                    string source = Value(item, "FullPath");
                    if (kind == "None" && !File.Exists(source)) continue;
                    string link = Value(item, "Link");
                    if (link.Length == 0) link = Value(item, "Identity").Replace('\\', '/');
                    var entry = new XElement(kind, new XAttribute("Include", source), new XElement("Link", link));
                    foreach (var key in new[] { "LogicalName", "ManifestResourceName", "CopyToOutputDirectory", "TargetPath" })
                        if (Value(item, key) is { Length: > 0 } value) entry.Add(new XElement(key, value));
                    entries.Add(entry);
                    if (kind == "Compile" && source.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(source) && File.ReadAllText(source).Contains("System.Configuration.ApplicationSettingsBase"))
                        needsSettings = true;
                }
            }

            var packages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in items.GetProperty("PackageReference").EnumerateArray())
                packages[Value(package, "Identity")] = Value(package, "Version");
            var configFile = Path.Combine(directory, "packages.config");
            bool hasPackageConfig = File.Exists(configFile);
            if (hasPackageConfig)
                foreach (var package in XDocument.Load(configFile).Root!.Elements("package"))
                    packages.TryAdd((string)package.Attribute("id")!, (string)package.Attribute("version")!);
            foreach (var reference in items.GetProperty("Reference").EnumerateArray())
            {
                var name = Value(reference, "Identity").Split(',')[0];
                if (name == "System.Configuration") needsSettings = true;
                var hint = Value(reference, "HintPath").Replace('\\', '/');
                if (hint.Length > 0 && !(hasPackageConfig && hint.Split('/').Contains("packages", StringComparer.OrdinalIgnoreCase)))
                    entries.Add(new XElement("Reference", new XAttribute("Include", name),
                        new XElement("HintPath", Path.GetFullPath(hint, directory))));
            }
            if (needsSettings) packages.TryAdd("System.Configuration.ConfigurationManager", "8.0.1");
            foreach (var (name, version) in packages)
            {
                if (string.IsNullOrEmpty(version)) throw new InvalidDataException($"{path}: no package version resolved for {name}.");
                entries.Add(new XElement("PackageReference", new XAttribute("Include", name), new XAttribute("Version", version)));
            }
            foreach (var reference in items.GetProperty("ProjectReference").EnumerateArray())
                entries.Add(new XElement("ProjectReference", new XAttribute("Include", ConvertProject(Value(reference, "FullPath")))));

            var xml = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"), settings, entries));
            GeneratedFiles.WriteXml(target, xml);
            return target;
        }
    }
}

internal static class GeneratedFiles
{
    public static void WriteXml(string path, XDocument document)
    {
        using var bytes = new MemoryStream();
        document.Save(bytes, SaveOptions.DisableFormatting);
        Write(path, bytes.ToArray());
    }

    public static void Write(string path, byte[] bytes)
    {
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }
}
