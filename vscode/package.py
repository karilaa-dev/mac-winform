#!/usr/bin/env python3
"""Bundle the extension and its native runtime in a local, installable VSIX."""
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZIP_DEFLATED, ZipFile
import argparse
import json
import mimetypes

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
OPC = "http://schemas.openxmlformats.org/package/2006/content-types"
VSIX = "http://schemas.microsoft.com/developer/vsx-schema/2011"


def manifest(pkg):
    root = ET.Element("PackageManifest", Version="2.0.0", xmlns=VSIX)
    meta = ET.SubElement(root, "Metadata")
    ET.SubElement(meta, "Identity", Id=pkg["name"], Version=pkg["version"], Publisher=pkg["publisher"], Language="en-US")
    ET.SubElement(meta, "DisplayName").text = pkg["displayName"]
    ET.SubElement(meta, "Description").text = pkg["description"]
    props = ET.SubElement(meta, "Properties")
    ET.SubElement(props, "Property", Id="Microsoft.VisualStudio.Code.Engine", Value=pkg["engines"]["vscode"])
    install = ET.SubElement(root, "Installation")
    ET.SubElement(install, "InstallationTarget", Id="Microsoft.VisualStudio.Code")
    ET.SubElement(root, "Dependencies")
    assets = ET.SubElement(root, "Assets")
    ET.SubElement(assets, "Asset", Type="Microsoft.VisualStudio.Code.Manifest", Path="extension/package.json", Addressable="true")
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def content_types(files):
    root = ET.Element("Types", xmlns=OPC)
    ET.SubElement(root, "Default", Extension="vsixmanifest", ContentType="text/xml")
    for name in sorted(files):
        mime = mimetypes.guess_type(name)[0] or "application/octet-stream"
        ET.SubElement(root, "Override", PartName="/extension/" + name, ContentType=mime)
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def main():
    pkg = json.loads((HERE / "package.json").read_text())
    default = ROOT / "dist" / (pkg["name"] + "-" + pkg["version"] + ".vsix")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", nargs="?", type=Path, default=default)
    dest = parser.parse_args().output
    files = {name: HERE / name for name in ["package.json", "extension.js", "run-adapter.js", "debug-build.js"]}
    for pattern in ["notices/*.txt", "bin/mac-winform", "bin/*.py", "runtime/*.cs", "runtime/*.csproj", "runtime/Resources/*.ico",
                    "native/*.h", "native/*.m", "build/analyzers/*.dll", "build/*.props", "build/*.targets"]:
        for file in ROOT.glob(pattern):
            files[file.relative_to(ROOT).as_posix()] = file
    if "bin/mac-winform" not in files or "runtime/MacWinForms.csproj" not in files:
        parser.error("Run this script from a complete mac-winform checkout.")
    dest.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(dest, "w", ZIP_DEFLATED) as archive:
        archive.writestr("extension.vsixmanifest", manifest(pkg))
        archive.writestr("[Content_Types].xml", content_types(files))
        for name, file in sorted(files.items()):
            archive.write(file, "extension/" + name)
    print(dest.resolve())


if __name__ == "__main__":
    main()
