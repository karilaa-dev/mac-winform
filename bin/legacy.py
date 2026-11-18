"""Project a classic C# project graph into a private stock-SDK build directory."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def write_if_changed(path, root):
    data = ET.tostring(root, encoding="utf-8", xml_declaration=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def project_graph(entry, configuration, artifacts, run_msbuild, launch_error):
    # Input paths and items come from MSBuild evaluation, which handles conditions,
    # imports, linked source files and the active Debug/Release configuration.
    built = {}
    properties = ("OutputType", "AssemblyName", "RootNamespace", "StartupObject", "DefineConstants",
                  "AllowUnsafeBlocks", "CheckForOverflowUnderflow", "LangVersion")
    items = ("Compile", "EmbeddedResource", "Content", "None", "Reference",
             "ProjectReference", "PackageReference", "COMReference")
    def convert(project):
        if project in built:
            return built[project]
        if project.suffix.lower() != ".csproj":
            raise launch_error(f"Classic project conversion requires C# source: {project}")
        raw = run_msbuild(["msbuild", str(project), "-nologo", "-p:Configuration=" + configuration,
                           "-getProperty:UsingMicrosoftNETSdk,TargetFrameworkIdentifier," + ",".join(properties), "-getItem:" + ",".join(items)], project.parent)
        info = json.loads(raw[raw.index("{"):])
        props, group = info["Properties"], info["Items"]
        uses_sdk = props.pop("UsingMicrosoftNETSdk").lower() == "true"
        target_kind = props.pop("TargetFrameworkIdentifier")
        if uses_sdk and target_kind != ".NETFramework":
            built[project] = project
            return project
        if target_kind != ".NETFramework":
            raise launch_error(f"Cannot convert the project type of {project.name}.")
        if group["COMReference"]:
            raise launch_error(f"{project.name} requires Windows COM components.")
        target = artifacts / "projects" / hashlib.sha256(str(project).encode()).hexdigest()[:12] / project.name
        built[project] = target
        target.parent.mkdir(parents=True, exist_ok=True)
        root = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
        settings = ET.SubElement(root, "PropertyGroup")
        for key, value in dict(TargetFramework="net8.0-windows", UseWindowsForms="true",
                               EnableDefaultItems="false", GenerateAssemblyInfo="false",
                               MacWinFormsClassic="true", **props).items():
            if value:
                ET.SubElement(settings, key).text = value
        entries = ET.SubElement(root, "ItemGroup")
        needs_settings = False
        for kind in ("Compile", "EmbeddedResource", "Content", "None"):
            for item in group[kind]:
                path = Path(item["FullPath"])
                if not path.exists() and kind == "None":
                    continue
                link = item.get("Link") or item["Identity"].replace("\\", "/")
                inc = ET.SubElement(entries, kind, Include=str(path))
                ET.SubElement(inc, "Link").text = link
                for name in ("LogicalName", "ManifestResourceName", "CopyToOutputDirectory", "TargetPath"):
                    if item.get(name):
                        ET.SubElement(inc, name).text = item[name]
                if kind == "Compile" and path.name.lower().endswith(".designer.cs") and path.is_file():
                    if "System.Configuration.ApplicationSettingsBase" in path.read_text(errors="replace"):
                        needs_settings = True
        packages = {}
        for item in group["PackageReference"]:
            packages[item["Identity"]] = item.get("Version")
        old_packages = project.parent / "packages.config"
        if old_packages.exists():
            for package in ET.parse(old_packages).getroot().findall("package"):
                packages.setdefault(package.attrib["id"], package.attrib["version"])
        for item in group["Reference"]:
            name = item["Identity"].split(",")[0]
            if name == "System.Configuration":
                needs_settings = True
            hint = item.get("HintPath", "").replace("\\", "/")
            if hint and not (old_packages.exists() and "packages" in Path(hint).parts):
                lib = ET.SubElement(entries, "Reference", Include=name)
                ET.SubElement(lib, "HintPath").text = str((project.parent / hint).resolve())
        if needs_settings:
            packages.setdefault("System.Configuration.ConfigurationManager","8.0.1")
        for name, version in packages.items():
            if not version:
                raise launch_error(f"{project.name}: no package version resolved for {name}.")
            ET.SubElement(entries, "PackageReference", Include=name, Version=version)
        for item in group["ProjectReference"]:
            ref = Path(item["FullPath"])
            ET.SubElement(entries,"ProjectReference",Include=str(convert(ref)))
        write_if_changed(target,root)
        return target
    return convert(entry)
