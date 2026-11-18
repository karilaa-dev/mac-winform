using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace MacWinForms.Tool;

internal static class Program
{
    private const string Help = """
        Build and run a C# WinForms application on macOS using the .NET SDK.

        Usage:
          dotnet winforms [run|build] [PROJECT | SOLUTION | DIRECTORY] [options] [-- APP_ARGS...]

        With no arguments, build and run the WinForms project in the current directory.
        Use 'build' to compile without starting the application.

        Options:
          --project <path>                  Project, solution, or directory
          -c, --configuration <name>        Build configuration (default: Debug)
          -f, --framework <tfm>             Original Windows target for a multi-target project
          --app-path-file <path>            Write the path of the compiled Mac executable
          --print-targets                   Print the MSBuild targets path
          --version                        Show the installed tool version
          -h, --help                       Show this help
        """;

    internal static int Main(string[] args)
    {
        try
        {
            if (args is ["resources", "prepare" or "wrap"])
            {
                ResourceConverter.Run(args[1]);
                return 0;
            }

            int separator = Array.IndexOf(args, "--");
            var options = separator < 0 ? args : args[..separator];
            var applicationArgs = separator < 0 ? [] : args[(separator + 1)..];
            if (options.Contains("--help") || options.Contains("-h"))
            {
                Console.WriteLine(Help);
                return 0;
            }
            if (options is ["--version"])
            {
                var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                Console.WriteLine(version?.InformationalVersion.Split('+')[0] ?? typeof(Program).Assembly.GetName().Version?.ToString());
                return 0;
            }
            if (options is ["--print-targets"])
            {
                Console.WriteLine(Launcher.TargetsPath);
                return 0;
            }
            var command = Command.Parse(options, applicationArgs);
            if (!OperatingSystem.IsMacOS()) throw new InvalidOperationException("This tool requires macOS.");
            return new Launcher(command).Execute();
        }
        catch (BuildFailure error) { return error.ExitCode; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception
                or ArgumentException or InvalidOperationException or JsonException or System.Xml.XmlException
                or FormatException or OverflowException or InvalidDataException)
        {
            Console.Error.WriteLine($"dotnet winforms: {error.Message}");
            return 1;
        }
    }
}

internal sealed record Command(bool BuildOnly, string Project, string Configuration, string? Framework,
    string? AppPathFile, string[] ApplicationArgs)
{
    internal static Command Parse(string[] args, string[] applicationArgs)
    {
        bool buildOnly = args.FirstOrDefault() == "build";
        int i = buildOnly || args.FirstOrDefault() == "run" ? 1 : 0;
        string? project = null, framework = null, appPathFile = null;
        string configuration = "Debug";
        string Value(string option) => ++i < args.Length && !string.IsNullOrWhiteSpace(args[i])
            ? args[i] : throw new ArgumentException($"{option} requires a value.");
        void SetProject(string value)
        {
            if (project != null) throw new ArgumentException("Specify one project or solution.");
            project = value;
        }

        for (; i < args.Length; i++)
        {
            var option = args[i];
            switch (option)
            {
                case "--project": SetProject(Value(option)); break;
                case "--configuration": case "-c": configuration = Value(option); break;
                case "--framework": case "-f": framework = Value(option); break;
                case "--app-path-file": appPathFile = Value(option); break;
                default:
                    if (option.StartsWith('-')) throw new ArgumentException($"Unknown option '{option}'. Run dotnet winforms --help for usage.");
                    SetProject(option);
                    break;
            }
        }
        if (buildOnly && applicationArgs.Length > 0) throw new ArgumentException("Application arguments require run.");
        return new Command(buildOnly, project ?? ".", configuration, framework, appPathFile, applicationArgs);
    }
}

internal sealed class BuildFailure(int exitCode) : Exception
{
    internal int ExitCode => exitCode;
}
