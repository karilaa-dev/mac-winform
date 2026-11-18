"""Run the native WinForms acceptance tests on macOS from an interactive login session."""
from pathlib import Path
import os
import subprocess
import sys
import tempfile
from xml.etree import ElementTree

TESTS = Path(__file__).resolve().parent


def install_tool(scratch, env):
    """Install this checkout's package into an isolated tool-path, including its payload."""
    feed = scratch / "feed"
    tools = scratch / "tools"
    tools.mkdir()
    # Developer checks run through sys.executable. The packaged tool and apps must
    # still work with no Python interpreter on PATH, including designer resx builds.
    for interpreter in ("python", "python3"):
        stub = tools / interpreter
        stub.write_text('#!/bin/sh\necho "Unexpected Python dependency" >&2\nexit 99\n')
        stub.chmod(0o755)
    env["PATH"] = str(tools) + os.pathsep + env["PATH"]
    config = scratch / "nuget.config"
    sources = ElementTree.Element("configuration")
    package_sources = ElementTree.SubElement(sources, "packageSources")
    ElementTree.SubElement(package_sources, "clear")
    ElementTree.SubElement(package_sources, "add", key="local", value=str(feed))
    ElementTree.ElementTree(sources).write(config, encoding="utf-8", xml_declaration=True)
    subprocess.run(["dotnet", "pack", TESTS.parent / "cli/MacWinForms.Tool.csproj",
                    "-c", "Release", "--artifacts-path", scratch / "pack", "-o", feed,
                    "-p:NuGetAudit=false"], env=env, check=True, timeout=240)
    subprocess.run(["dotnet", "tool", "install", "MacWinForms.Tool", "--tool-path", tools,
                    "--configfile", config], check=True, timeout=90,
                   env=dict(env, NUGET_PACKAGES=str(scratch / "packages")))


def main():
    if sys.platform != "darwin":
        sys.exit("This verification requires macOS and AppKit.")
    with tempfile.TemporaryDirectory(prefix="mac-winform-verification-") as directory:
        scratch = Path(directory).resolve()
        env = dict(os.environ)
        env.setdefault("MAC_WINFORM_CACHE",str(scratch / "cache"))
        install_tool(scratch, env)

        def run(*args):
            subprocess.run(args,env=env,check=True,timeout=240)

        run(sys.executable,TESTS / "cli_integration.py")
        suites = sorted(p for p in TESTS.iterdir() if p.is_dir() and (p / (p.name + ".csproj")).is_file())
        for suite in suites:
            print(f"Testing {suite.name}",flush=True)
            if (suite / "verify.py").exists():
                run(sys.executable,suite / "verify.py")
            elif suite.name == "Gallery":
                output = scratch / "gallery"
                output.mkdir()
                run("dotnet","winforms","run",suite,"--","--probe",output)
                run(sys.executable,suite / "compare.py",TESTS.parent / "docs/gallery/windows/state.json",output / "state.json")
            elif suite.name == "Printing":
                proof = scratch / "print.pdf"
                run("dotnet","winforms","run",suite,"--","--pdf",proof)
                if not proof.read_bytes().startswith(b"%PDF-"):
                    sys.exit("PrintDocument did not create a PDF.")
            else:
                run("dotnet","winforms","run",suite)
        print(f"Passed {len(suites)} native fixtures and both launcher tests.",flush=True)


if __name__ == "__main__":
    main()
