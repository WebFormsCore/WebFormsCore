using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.Designer;
using WebFormsCore.Nodes;

namespace WebFormsCore.Compiler;

public record struct CompileResult(CSharpCompilation Compilation, string Type);

public class PageCompiler
{
    public static CompileResult Compile(string path)
    {
        var assemblyName = path
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('.', '_')
            .Replace(':', '_')
            .ToLowerInvariant();

        var defaultCompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: true,
            platform: Platform.AnyCpu
        );

        var references = new HashSet<MetadataReference>();
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        references.Add(MetadataReference.CreateFromFile(assembly.Location));

        foreach (var referencedAssembly in GetAssemblies())
        {
            references.Add(MetadataReference.CreateFromFile(referencedAssembly.Location));
        }

#if NETFRAMEWORK
        if (System.Web.HttpRuntime.BinDirectory is {} binDirectory)
        {
            foreach (var dllFile in Directory.GetFiles(binDirectory, "*.dll"))
            {
                references.Add(MetadataReference.CreateFromFile(dllFile));
            }
        }
#endif

        var compilation = CSharpCompilation.Create(
            $"WebForms_{assemblyName}",
            references: references,
            options: defaultCompilationOptions
        );

        var text= File.ReadAllText(path);
        var type = DesignerType.Parse(compilation, path, text);

        if (type == null)
        {
            return default;
        }

        var rootType = compilation.GetTypeByMetadataName(type.Namespace == null ? type.Name : type.Namespace + "." + type.Name);
        var ns = type.Namespace ?? "Default";
        var code = Create(ns, assemblyName, $"{type.Namespace}.{type.Name}", type.Root, rootType);

        compilation = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(code)
        );

        return new CompileResult(compilation, $"{ns}.{assemblyName}");
    }

    public static List<Assembly> GetAssemblies()
    {
        var returnAssemblies = new List<Assembly>();
        var loadedAssemblies = new HashSet<string>();
        var assembliesToCheck = new Queue<Assembly>();

        assembliesToCheck.Enqueue(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

        while(assembliesToCheck.Any())
        {
            var assemblyToCheck = assembliesToCheck.Dequeue();

            foreach(var reference in assemblyToCheck.GetReferencedAssemblies())
            {
                if (!loadedAssemblies.Contains(reference.FullName))
                {
                    var assembly = Assembly.Load(reference);
                    assembliesToCheck.Enqueue(assembly);
                    loadedAssemblies.Add(reference.FullName);
                    returnAssemblies.Add(assembly);
                }
            }
        }

        return returnAssemblies;
    }
    
    public static string Create(
        string ns,
        string name,
        string baseType,
        RootNode node,
        INamedTypeSymbol? namedType)
    {
        var sb = new StringBuilder();
        var context = new CompileContext(sb)
        {
            Type = namedType
        };

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using WebFormsCore.UI;");
        sb.AppendLine("using WebFormsCore.UI.WebControls;");
        sb.AppendLine();
        
        sb.Append("namespace ");
        sb.AppendLine(ns);
        sb.AppendLine("{");
        {
            sb.Append("partial class ");
            sb.Append(name);
            sb.Append(" : ");
            sb.AppendLine(baseType);
            sb.AppendLine("{");
            {
                node.WriteClass(context);
                
                sb.AppendLine("protected override void FrameworkInitialize()");
                sb.AppendLine("{");
                {
                    sb.AppendLine("base.FrameworkInitialize();");
                    node.Write(context);
                }
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }
}
