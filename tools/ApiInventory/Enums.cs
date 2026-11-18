using System.Reflection;
using System.Text;

internal static class Enums
{
    // Keep the portable constant definitions in sync with the chosen Windows Desktop reference pack.
    internal static void Write(string path, IEnumerable<Type> reference, Dictionary<string,Type> implementation)
    {
        var output = new StringBuilder("// Generated from Microsoft's .NET Windows Desktop reference pack by tools/ApiInventory.\n// See notices/dotnet-winforms.txt.\n");
        var types = reference.Where(t => t.IsEnum && !t.IsNested &&
            (!implementation.TryGetValue(t.FullName!,out Type? existing) ||
             existing.CustomAttributes.Any(a => a.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute")));
        foreach (var group in types.GroupBy(t=>t.Namespace).OrderBy(g=>g.Key))
        {
            output.AppendLine($"namespace {group.Key}").AppendLine("{");
            foreach (var type in group.OrderBy(t=>t.Name))
            {
                output.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"MacWinForms.ApiInventory\",\"1\")]");
                if (type.CustomAttributes.Any(a=>a.AttributeType.FullName == "System.FlagsAttribute"))
                    output.AppendLine("    [global::System.Flags]");
                output.AppendLine($"    public enum {type.Name} : global::{type.GetEnumUnderlyingType().FullName}").AppendLine("    {");
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    output.AppendLine($"        @{field.Name} = {field.GetRawConstantValue()},");
                output.AppendLine("    }");
            }
            output.AppendLine("}");
        }
        File.WriteAllText(path,output.ToString());
    }
}
