using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Windows.Forms;

public class ApplicationContext : IDisposable
{
    private Form? mainForm;
    public ApplicationContext() { }
    public ApplicationContext(Form? mainForm) => MainForm = mainForm;
    public Form? MainForm
    {
        get => mainForm;
        set
        {
            if (mainForm == value) return;
            if (mainForm != null) mainForm.FormClosed -= MainClosed;
            mainForm = value;
            if (mainForm != null) mainForm.FormClosed += MainClosed;
        }
    }
    private void MainClosed(object? sender,FormClosedEventArgs e) => OnMainFormClosed(sender,e);
    public object? Tag { get; set; }
    public event EventHandler? ThreadExit;
    protected virtual void OnMainFormClosed(object? sender,EventArgs e) => ExitThreadCore();
    protected virtual void ExitThreadCore() => ThreadExit?.Invoke(this,EventArgs.Empty);
    public void ExitThread() => ExitThreadCore();
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        var old = MainForm;
        MainForm = null;
        old?.Dispose();
    }
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}

public class FormCollection : ReadOnlyCollectionBase
{
    internal void Add(Form f) { if (!InnerList.Contains(f)) InnerList.Add(f); }
    internal void Remove(Form f) => InnerList.Remove(f);
    public Form? this[int index] => (Form?)InnerList[index];
    public Form? this[string? name] => InnerList.Cast<Form>().FirstOrDefault(f=>string.Equals(name,f.Name,StringComparison.OrdinalIgnoreCase));
}

public static partial class Application
{
    private static readonly FormCollection forms = new();
    private static ApplicationContext? running;
    public static FormCollection OpenForms => forms;
    public static bool MessageLoop => running != null;
    public static string ExecutablePath => Environment.ProcessPath ?? "";
    public static string StartupPath => Path.GetDirectoryName(ExecutablePath) ?? AppContext.BaseDirectory;
    public static string ProductName => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";
    public static string ProductVersion => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
    public static string CompanyName => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
    public static event EventHandler? ApplicationExit, ThreadExit, Idle;
    public static void Run() => Run(new ApplicationContext());
    public static void Run(ApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (running != null) throw new InvalidOperationException("A message loop is already running on this thread.");
        Initialize();
        bool done = false;
        void OnExit(object? sender,EventArgs e) => done = true;
        running = context;
        context.ThreadExit += OnExit;
        try
        {
            context.MainForm?.Show();
            while (!done && context.MainForm?.IsDisposed != true)
            {
                Idle?.Invoke(null,EventArgs.Empty);
                if (!done) Native.mf_pump();
            }
        }
        finally
        {
            context.ThreadExit -= OnExit;
            running = null;
            context.Dispose();
            ThreadExit?.Invoke(null,EventArgs.Empty);
            ApplicationExit?.Invoke(null,EventArgs.Empty);
        }
    }
    public static void ExitThread() => running?.ExitThread();
}
