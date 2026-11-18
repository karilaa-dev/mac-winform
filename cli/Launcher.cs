using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Win32.SafeHandles;

namespace MacWinForms.Tool;

internal sealed partial class Launcher(Command command)
{
    private readonly Dictionary<(string Project, string? Framework), ProjectInfo> evaluated = new();
    private static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host && File.Exists(host) ? host : "dotnet";
    internal static string TargetsPath => Path.Combine(AppContext.BaseDirectory, "build", "MacWinForms.targets");
    private static readonly string[] ProjectExtensions = [".csproj", ".vbproj", ".fsproj"];
    private static readonly HashSet<string> SkipDirectories = new(StringComparer.OrdinalIgnoreCase)
        { "bin", "obj", "node_modules", "packages", "artifacts" };

    [GeneratedRegex(@"^net(\d+)\.\d+-windows(?:\d+(?:\.\d+){0,3})?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WindowsTarget();

    internal int Execute()
    {
        var (entry, framework) = SelectProject();
        var match = WindowsTarget().Match(framework);
        if (match.Success && int.Parse(match.Groups[1].Value) < 8)
            Console.WriteLine($"Retargeting {framework} to net8.0 for the native Mac runtime.");
        string executable = Build(entry, framework);
        if (command.AppPathFile != null) File.WriteAllText(command.AppPathFile, executable + "\n");
        Console.WriteLine($"Mac app host: {executable}");
        if (command.BuildOnly) return 0;
        Environment.CurrentDirectory = Parent(entry);
        return ExecApplication(executable, command.ApplicationArgs);
    }

    private static string Parent(string path) => Path.GetDirectoryName(path)!;
    private static bool IsProject(string path) => ProjectExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    private static bool IsSolution(string path) => Path.GetExtension(path).ToLowerInvariant() is ".sln" or ".slnx";

    private static string FullPath(string path)
    {
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),path[1..].TrimStart('/'));
        var full = Path.GetFullPath(path);
        var pointer = realpath(full, IntPtr.Zero);
        if (pointer == IntPtr.Zero)
        {
            // Resolve symlinks in the existing prefix of a fresh cache path.
            string? directory = Path.GetDirectoryName(full);
            return directory == null ? full : Path.Combine(FullPath(directory), Path.GetFileName(full));
        }
        try { return Marshal.PtrToStringUTF8(pointer)!; }
        finally { free(pointer); }
    }

    private static IEnumerable<string> Candidates(string path)
    {
        if (File.Exists(path) && IsProject(path)) return [path];
        if (File.Exists(path) && IsSolution(path))
        {
            string listing = DotnetOutput(Parent(path), "sln", path, "list");
            return listing.Split('\n').Select(line => Path.Combine(Parent(path), line.Trim().Replace('\\', '/')))
                .Where(item => IsProject(item) && File.Exists(item)).Select(FullPath);
        }
        if (!Directory.Exists(path)) throw new ArgumentException($"Project, solution or folder not found: {path}");
        var files = Directory.GetFiles(path);
        var projects = files.Where(IsProject).ToArray();
        if (projects.Length > 0) return projects;
        var solutions = files.Where(IsSolution).ToArray();
        if (solutions.Length == 1) return Candidates(solutions[0]);
        return DescendantProjects(path);
    }

    private static IEnumerable<string> DescendantProjects(string path)
    {
        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            string name = Path.GetFileName(directory);
            if (name.StartsWith('.') || SkipDirectories.Contains(name) ||
                File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint)) continue;
            foreach (var file in Directory.EnumerateFiles(directory).Where(IsProject)) yield return file;
            foreach (var file in DescendantProjects(directory)) yield return file;
        }
    }

    private (string Project, string Framework) SelectProject()
    {
        var path = FullPath(command.Project);
        var projects = Candidates(path).Select(FullPath).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (projects.Length == 0) throw new ArgumentException($"No .NET project was found in {path}.");
        if (projects.Length == 1) return (projects[0], EntryFramework(projects[0]));
        var choices = new List<(string Project, string Framework)>();
        var errors = new List<string>();
        foreach (var project in projects)
        {
            try { choices.Add((project, EntryFramework(project))); }
            catch (InvalidOperationException e) { errors.Add(e.Message); }
        }
        if (choices.Count == 1) return choices[0];
        if (choices.Count == 0) throw new InvalidOperationException("No runnable WinForms project found.\n" + string.Join('\n', errors));
        throw new InvalidOperationException("Choose a project by passing its path:\n" + string.Join('\n', choices.Select(c => c.Project)));
    }

    private sealed record ProjectInfo(JsonElement Properties, string[] References, bool HasFormsReference)
    {
        internal string Get(string property) => Properties.GetProperty(property).GetString() ?? "";
        internal bool IsTrue(string property) => string.Equals(Get(property), "true", StringComparison.OrdinalIgnoreCase);
        internal bool IsExecutable => Get("OutputType").ToLowerInvariant() is "exe" or "winexe";
        internal string[] Frameworks => (Get("TargetFrameworks") is { Length: > 0 } list ? list : Get("TargetFramework"))
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private ProjectInfo Evaluate(string project, string? framework = null)
    {
        if (evaluated.TryGetValue((project, framework), out var result)) return result;
        var args = new List<string> { "msbuild", project, "-nologo",
            "-getProperty:TargetFramework,TargetFrameworks,UseWindowsForms,OutputType,UseWPF,TargetFrameworkIdentifier,TargetFrameworkVersion",
            "-getItem:ProjectReference,Reference", "-p:Configuration=" + command.Configuration };
        if (framework != null) args.Add("-p:TargetFramework=" + framework);
        var json = ReadJson(DotnetOutput(Parent(project), args.ToArray()));
        var items = json.GetProperty("Items");
        var references = items.GetProperty("ProjectReference").EnumerateArray()
            .Select(item => FullPath(item.GetProperty("FullPath").GetString()!)).ToArray();
        bool forms = items.GetProperty("Reference").EnumerateArray()
            .Any(item => item.GetProperty("Identity").GetString()?.Split(',')[0].Equals("System.Windows.Forms", StringComparison.OrdinalIgnoreCase) == true);
        return evaluated[(project, framework)] = new ProjectInfo(json.GetProperty("Properties"), references, forms);
    }

    private string EntryFramework(string project)
    {
        var info = Evaluate(project);
        bool Matches(string value) => command.Framework == null || string.Equals(value, command.Framework, StringComparison.OrdinalIgnoreCase);
        if (info.Get("TargetFrameworkIdentifier") == ".NETFramework" && info.IsExecutable &&
            Path.GetExtension(project).Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
            (info.HasFormsReference || info.IsTrue("UseWindowsForms")) && info.Get("TargetFrameworkVersion").StartsWith("v4.", StringComparison.OrdinalIgnoreCase))
        {
            var classic = "net" + info.Get("TargetFrameworkVersion")[1..].Replace(".", "");
            if (Matches(classic)) return classic;
        }
        foreach (var framework in info.Frameworks)
        {
            var match = WindowsTarget().Match(framework);
            if (!match.Success || !Matches(framework) || int.Parse(match.Groups[1].Value) < 5) continue;
            var selected = info.Get("TargetFrameworks").Length == 0 ? info : Evaluate(project, framework);
            if (!selected.IsExecutable || !selected.IsTrue("UseWindowsForms")) continue;
            if (selected.IsTrue("UseWPF")) throw new InvalidOperationException($"{Path.GetFileName(project)} also requires WPF, which the Mac runtime does not implement.");
            return framework;
        }
        var suffix = command.Framework == null ? "" : " for " + command.Framework;
        throw new InvalidOperationException($"{Path.GetFileName(project)} has no matching WinForms executable target{suffix}.");
    }

    private static (Version Net, Version Platform) Versions(string framework)
    {
        var parts = framework[3..].ToLowerInvariant().Split("-windows");
        var platform = (parts[1].Length == 0 ? "7.0" : parts[1]).Split('.').Select(int.Parse).ToList();
        while (platform.Count < 4) platform.Add(0);
        return (Version.Parse(parts[0]), new Version(platform[0],platform[1],platform[2],platform[3]));
    }

    private XElement FrameworkMap(string entry, string framework)
    {
        var mapping = new Dictionary<string,string> { [entry] = framework };
        var pending = new Stack<(string Project,string Framework)>();
        var seen = new HashSet<(string Project,string Framework)>();
        pending.Push((entry,framework));
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node)) continue;
            var parent = Versions(WindowsTarget().IsMatch(node.Framework) ? node.Framework : framework);
            foreach (var reference in Evaluate(node.Project, node.Framework).References)
            {
                var frameworks = Evaluate(reference).Frameworks;
                var selected = frameworks.Where(t => WindowsTarget().IsMatch(t)).Select(t => (Tfm:t, Versions:Versions(t)))
                    .Where(t => t.Versions.Net <= parent.Net && t.Versions.Platform <= parent.Platform)
                    .OrderByDescending(t => t.Tfm.Equals(node.Framework, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(t => t.Versions).Select(t => t.Tfm).FirstOrDefault();
                if (selected != null)
                {
                    if (mapping.TryGetValue(reference, out var prior) && prior != selected)
                        throw new InvalidOperationException($"{reference} requires two Windows framework variants in one build: {prior} and {selected}.");
                    mapping[reference] = selected;
                    pending.Push((reference,selected));
                }
                else
                {
                    foreach (var target in frameworks.Where(f => !f.Contains('-'))) pending.Push((reference,target));
                }
            }
        }
        return new XElement("Project", mapping.OrderBy(k => k.Key, StringComparer.Ordinal).Select(item =>
            new XElement("PropertyGroup", new XAttribute("Condition",
                $"'$(MSBuildProjectFullPath)' == '{item.Key.Replace("%","%25").Replace("'","%27")}'"),
                new XElement("TargetFramework",item.Value))));
    }

    private string Build(string entry, string framework)
    {
        var cache = FullPath(Environment.GetEnvironmentVariable("MAC_WINFORM_CACHE") ?? "~/Library/Caches/mac-winform");
        var directory = Parent(entry);
        if (cache == directory || cache.StartsWith(directory + Path.DirectorySeparatorChar,StringComparison.Ordinal))
            throw new InvalidOperationException("Set MAC_WINFORM_CACHE to a directory outside the application project.");
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entry)))[..16].ToLowerInvariant();
        var artifacts = Path.Combine(cache, Path.GetFileNameWithoutExtension(entry) + "-" + key, framework);
        Directory.CreateDirectory(artifacts);
        using var buildLock = AcquireLock(Path.Combine(artifacts, "build.lock"));
        if (!WindowsTarget().IsMatch(framework))
        {
            Console.WriteLine($"Retargeting classic C# source from {framework} to the installed .NET 8 runtime.");
            entry = LegacyProjects.Convert(entry, command.Configuration, artifacts);
            framework = "net8.0-windows";
        }

        var props = Path.Combine(artifacts, "SelectedFrameworks.props");
        var contents = FrameworkMap(entry, framework).ToString();
        if (!File.Exists(props) || File.ReadAllText(props) != contents) File.WriteAllText(props, contents);
        var launchFile = Path.Combine(artifacts, $"app-path-{Guid.NewGuid():N}");
        try
        {
            var args = new[] { "build", entry, "--configuration", command.Configuration, "--artifacts-path", artifacts,
                "-p:CustomAfterMicrosoftCommonProps=" + props,
                "-p:BeforeTargetFrameworkInferenceTargets=" + TargetsPath,
                "-p:MacWinFormsEntry=" + entry, "-p:MacWinFormsLaunchFile=" + launchFile,
                "-p:MacWinFormsToolAssembly=" + typeof(Program).Assembly.Location };
            using var build = Process.Start(StartInfo(Dotnet, directory, args))!;
            build.WaitForExit();
            if (build.ExitCode != 0) throw new BuildFailure(build.ExitCode);
            if (!File.Exists(launchFile)) throw new InvalidOperationException("MSBuild did not report an app host. Check your project's custom SDK targets.");
            var executable = File.ReadAllText(launchFile).Trim();
            if (!File.Exists(executable) || access(executable, 1) != 0)
                throw new InvalidOperationException($"MSBuild reported an app host that cannot be launched: {executable}");
            return RefreshAppHost(executable);
        }
        finally { File.Delete(launchFile); }
    }

    private static string RefreshAppHost(string executable)
    {
        // Publish the patched apphost on a new inode to avoid a stale macOS signature.
        // A flat executable ending in .app is interpreted as an application bundle.
        var target = executable.EndsWith(".app",StringComparison.OrdinalIgnoreCase) ? executable + "-macos" : executable;
        var staging = Path.Combine(Parent(executable), ".mac-winform-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.Copy(executable,staging);
            File.Move(staging,target,true);
            return target;
        }
        finally { File.Delete(staging); }
    }

    private sealed class BuildLock : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal BuildLock(IntPtr file) : base(true) => SetHandle(file);
        internal int Descriptor => fileno(handle);

        protected override bool ReleaseHandle()
        {
            flock(Descriptor,8); // LOCK_UN
            return fclose(handle) == 0;
        }
    }

    private static BuildLock AcquireLock(string path)
    {
        // FileStream applies a nonblocking share lock while opening. Open the lockfile
        // directly so another build waits for the exclusive lock instead of failing.
        var file = fopen(path, "ae"); // FD_CLOEXEC on Darwin
        if (file == IntPtr.Zero) throw new IOException($"Cannot open {path}.",new Win32Exception(Marshal.GetLastPInvokeError()));
        var buildLock = new BuildLock(file);
        while (flock(buildLock.Descriptor,2) != 0) // LOCK_EX
        {
            int error = Marshal.GetLastPInvokeError();
            if (error == 4) continue; // EINTR
            buildLock.Dispose();
            throw new IOException($"Cannot lock {path}.", new Win32Exception(error));
        }
        return buildLock;
    }

    internal static JsonElement ReadJson(string output)
    {
        int start = output.IndexOf('{');
        if (start < 0) throw new InvalidOperationException($"MSBuild did not return project information. Install .NET SDK 8 or later.\n{output}");
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(output[start..]));
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    private static ProcessStartInfo StartInfo(string executable, string directory, IEnumerable<string> arguments)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = false };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }

    internal static string DotnetOutput(string directory, params string[] arguments)
    {
        var info = StartInfo(Dotnet, directory, arguments);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(output,errors);
        if (process.ExitCode != 0) throw new InvalidOperationException((errors.Result + output.Result).Trim());
        return output.Result;
    }

    private static int ExecApplication(string path, string[] arguments)
    {
        // Replace the launcher so the app keeps the terminal, signals and exit status.
        var strings = new[] {path}.Concat(arguments).Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        var pointers = Marshal.AllocHGlobal((strings.Length + 1) * IntPtr.Size);
        try
        {
            Marshal.Copy(strings,0,pointers,strings.Length);
            Marshal.WriteIntPtr(pointers,strings.Length * IntPtr.Size,IntPtr.Zero);
            execv(path,pointers);
            throw new Win32Exception(Marshal.GetLastPInvokeError(),$"Could not start {path}.");
        }
        finally
        {
            Marshal.FreeHGlobal(pointers);
            foreach (var s in strings) Marshal.FreeCoTaskMem(s);
        }
    }

    [DllImport("libSystem.B.dylib", SetLastError = true)] private static extern IntPtr fopen(string path, string mode);
    [DllImport("libSystem.B.dylib")] private static extern int fileno(IntPtr file);
    [DllImport("libSystem.B.dylib")] private static extern int fclose(IntPtr file);
    [DllImport("libSystem.B.dylib", SetLastError = true)] private static extern int flock(int file, int operation);
    [DllImport("libSystem.B.dylib", SetLastError = true)] private static extern int execv(string path, IntPtr arguments);
    [DllImport("libSystem.B.dylib")] private static extern int access(string path, int mode);
    [DllImport("libSystem.B.dylib")] private static extern IntPtr realpath(string path, IntPtr result);
    [DllImport("libSystem.B.dylib")] private static extern void free(IntPtr pointer);
}
