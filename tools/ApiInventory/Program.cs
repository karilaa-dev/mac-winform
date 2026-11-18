using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("Usage: ApiInventory <WindowsDesktop ref/net8.0 folder> <MacWinForms.dll> [report.json] [portable-enums.cs]");
    return 2;
}
string windows = Path.GetFullPath(args[0]), mac = Path.GetFullPath(args[1]);
var system = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
var desktop = Directory.GetFiles(windows, "*.dll");
using var reference = Context(desktop);
using var implementation = Context([mac]);
var expected = new Dictionary<string,Type>(StringComparer.Ordinal);
foreach (string library in new[]{"System.Windows.Forms.dll","System.Windows.Forms.Primitives.dll"})
    foreach (var type in reference.LoadFromAssemblyPath(Path.Combine(windows,library)).GetTypes().Where(InScope))
        expected.Add(type.FullName!,type);
var actual = implementation.LoadFromAssemblyPath(mac).GetTypes().Where(InScope).ToDictionary(t=>t.FullName!);
var report = new List<object>();
int matchedTypes = 0, matchedMembers = 0, totalMembers = 0;
foreach (var (name, type) in expected.OrderBy(x=>x.Key))
{
    var contract = Members(type);
    bool present = actual.TryGetValue(name,out var other);
    var found = present ? Members(other!) : new HashSet<string>();
    var missing = contract.Except(found).Order().ToArray();
    if (present) matchedTypes++;
    totalMembers += contract.Count;
    matchedMembers += contract.Count - missing.Length;
    bool control = type.Namespace == "System.Windows.Forms" && type.IsClass && !type.IsAbstract &&
        type.GetConstructor(Type.EmptyTypes) != null &&
        reference.LoadFromAssemblyName("System.Windows.Forms").GetType("System.Windows.Forms.Control")!.IsAssignableFrom(type);
    report.Add(new { name, present, constructibleControl = control, declaredMembers = contract.Count, missing });
}
Console.WriteLine($"Public type names: {matchedTypes}/{expected.Count}. Declared signature matches: {matchedMembers}/{totalMembers}.");
if (args.Length == 4) Enums.Write(args[3],expected.Values,actual);
if (args.Length >= 3)
    File.WriteAllText(args[2],JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
return 0;

MetadataLoadContext Context(string[] first)
{
    var assemblies = first.Concat(system).DistinctBy(p=>Path.GetFileName(p),StringComparer.OrdinalIgnoreCase);
    return new MetadataLoadContext(new PathAssemblyResolver(assemblies),"System.Private.CoreLib");
}
static bool InScope(Type t) => t.IsVisible && t.Namespace is {} n &&
    (n == "System.Windows.Forms" || n.StartsWith("System.Windows.Forms.",StringComparison.Ordinal));
static string TypeName(Type t)
{
    if (t.IsGenericParameter) return (t.DeclaringMethod == null ? "!" : "!!") + t.GenericParameterPosition;
    if (t.HasElementType)
        return TypeName(t.GetElementType()!) + (t.IsArray ? "[" + new string(',',t.GetArrayRank()-1) + "]" : t.IsByRef ? "&" : "*");
    if (t.IsGenericType)
        return t.GetGenericTypeDefinition().FullName + "<" + string.Join(",",t.GetGenericArguments().Select(TypeName)) + ">";
    return t.FullName ?? t.Name;
}
static HashSet<string> Members(Type type)
{
    // Accessor methods encode property types, indexers, getter/setter presence and event delegate signatures.
    const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    var result = new HashSet<string>();
    foreach (MethodBase method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
    {
        if (!method.IsPublic && !method.IsFamily && !method.IsFamilyOrAssembly) continue;
        var args = string.Join(",",method.GetParameters().Select(p=> (p.IsOut ? "out " : "") + TypeName(p.ParameterType)));
        var ret = method is MethodInfo m ? TypeName(m.ReturnType) : "void";
        var generic = method.IsGenericMethod ? "`" + method.GetGenericArguments().Length : "";
        result.Add($"{(method.IsPublic?"public":"protected")} {(method.IsStatic?"static":"instance")} {ret} {method.Name}{generic}({args})");
    }
    foreach (FieldInfo field in type.GetFields(flags))
    {
        if (!(field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly) || field.IsSpecialName) continue;
        result.Add($"{(field.IsPublic?"public":"protected")} {(field.IsStatic?"static":"instance")} {TypeName(field.FieldType)} {field.Name}" +
            (field.IsLiteral ? " = " + field.GetRawConstantValue() : ""));
    }
    return result;
}
