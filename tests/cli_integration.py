"""Exercise the stock SDK with an unchanged, multi-project WinForms application."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


@unittest.skipUnless(sys.platform == "darwin", "The app host and UI bridge require macOS.")
class LauncherIntegration(unittest.TestCase):
    def test_classic_project_with_linked_source_and_sdk_reference(self):
        with tempfile.TemporaryDirectory(prefix="classic forms ") as directory:
            work = Path(directory).resolve()
            app, lib = work / "src" / "Desktop", work / "src" / "Model"
            app.mkdir(parents=True)
            lib.mkdir()
            (lib / "Model.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
            (lib / "Model.cs").write_text('public class Model { public const string Title = "Classic"; }')
            (app.parent / "Entry.cs").write_text("""using System;
using System.Windows.Forms;
class Entry {
    static void Main() {
#if ORIGINAL_DEFINE
        Console.WriteLine(new Form { Text = Model.Title }.Text);
#else
#error Configuration did not reach linked source
#endif
    }
}""")
            (app / "Old.csproj").write_text("""<Project ToolsVersion="15.0"
 xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
 <PropertyGroup><TargetFrameworkVersion>v4.8</TargetFrameworkVersion><AssemblyName>Original.App</AssemblyName>
 <OutputType>WinExe</OutputType><DefineConstants>ORIGINAL_DEFINE</DefineConstants>
 <OutputPath>bin/</OutputPath></PropertyGroup><ItemGroup>
 <Reference Include="System"/><Reference Include="System.Windows.Forms"/>
 <Compile Include="../Entry.cs"><Link>Program.cs</Link></Compile>
 <ProjectReference Include="../Model/Model.csproj" /></ItemGroup>
 <Import Project="$(MSBuildToolsPath)/Microsoft.CSharp.targets"/></Project>""")
            snapshot = {p:p.read_bytes() for p in app.parent.rglob("*") if p.is_file()}
            env = dict(os.environ, MAC_WINFORM_CACHE=str(work / "build outputs"))
            result = subprocess.run(["dotnet", "winforms"], cwd=app, env=env,
                                    text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=120)
            self.assertEqual(result.returncode,0,result.stdout)
            self.assertEqual(result.stdout.splitlines()[-1],"Classic")
            self.assertEqual(snapshot,{p:p.read_bytes() for p in app.parent.rglob("*") if p.is_file()})

    def test_native_run_honors_project_graph_and_arguments(self):
        with tempfile.TemporaryDirectory(prefix="native forms ") as directory:
            work = Path(directory).resolve()
            app = work / "Solution" / "App"
            library = work / "Solution" / "Common"
            app.mkdir(parents=True)
            library.mkdir()
            sdk = subprocess.check_output(["dotnet", "--version"], text=True).strip()
            (app.parent / "global.json").write_text(json.dumps({"sdk": {"version": sdk}}))
            (app.parent / "Directory.Build.props").write_text(
                "<Project><PropertyGroup><AssemblyTitle>Inherited title</AssemblyTitle></PropertyGroup></Project>")
            (app / "Demo.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net6.0-windows</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>Custom.Host</AssemblyName>
    <RootNamespace>Example.Application</RootNamespace>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetFramework)' == 'net6.0-windows'">
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="../Common/Common.csproj" /></ItemGroup>
</Project>""")
            (library / "Common.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFrameworks>net5.0;net5.0-windows</TargetFrameworks></PropertyGroup>
  <PropertyGroup Condition="'$(TargetFramework)' == 'net5.0-windows'">
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
</Project>""")
            (library / "CommonForm.cs").write_text("public class CommonForm : System.Windows.Forms.Form {}")
            (app / "Program.cs").write_text("""
using System.Reflection;
using System.Text.Json;
namespace Example.Application {
class Program {
    static void Main(string[] args) {
        ApplicationConfiguration.Initialize();
        // Touch both injected namespaces and the generated SDK configuration class.
        Console.WriteLine(JsonSerializer.Serialize(new {
            baseType = typeof(CommonForm).BaseType == typeof(Form),
            config = typeof(ApplicationConfiguration).FullName,
            point = new Point(7, 9).X,
            title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title,
            cwd = Environment.CurrentDirectory,
            args
        }));
        if (Array.IndexOf(args, "--child-exit") >= 0) Environment.ExitCode = 19;
    }
}
}
""")
            inputs = {p: p.read_bytes() for p in app.parent.rglob("*") if p.is_file()}
            env = dict(os.environ, MAC_WINFORM_CACHE=str(work / "build outputs"))
            app_path_file = work / "built app path"
            forwarded = ["hello world", "--project", "$HOME", "--child-exit"]
            result = subprocess.run(["dotnet", "winforms", "run", "--project", str(app.parent),
                                     "--app-path-file", str(app_path_file), "--", *forwarded],
                                    cwd=work, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,timeout=120)
            self.assertEqual(result.returncode, 19, result.stdout)
            payload = json.loads(result.stdout.splitlines()[-1])
            self.assertEqual(payload, {"baseType": True, "config": "Example.Application.ApplicationConfiguration",
                                       "point": 7, "title": "Inherited title", "cwd": str(app),
                                       "args": forwarded})
            exe = Path(app_path_file.read_text().strip())
            self.assertEqual(exe.name, "Custom.Host")
            runtime = json.loads((exe.parent / (exe.name + ".runtimeconfig.json")).read_text())
            self.assertEqual(runtime["runtimeOptions"]["tfm"], "net8.0")
            self.assertIn("Mach-O", subprocess.check_output(["file", str(exe)], text=True))
            self.assertTrue((exe.parent / "libMacForms.dylib").is_file())
            self.assertEqual(inputs, {p: p.read_bytes() for p in app.parent.rglob("*") if p.is_file()})


if __name__ == "__main__":
    unittest.main()
