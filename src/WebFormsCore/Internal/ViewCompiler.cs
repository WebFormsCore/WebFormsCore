using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using WebFormsCore.Nodes;

namespace WebFormsCore.Compiler;

internal record struct ViewCompileResult(Compilation Compilation, RootNode Type);

internal static class ViewCompiler
{
    private static IReadOnlyList<MetadataReference>? _references;
    private static readonly object ReferencesLock = new();

    public static ViewCompileResult Compile(string path, string? text = null)
    {
        var tempAssemblyName = path
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('.', '_')
            .Replace(':', '_')
            .ToLowerInvariant();

        text ??= File.ReadAllText(path).ReplaceLineEndings("\n");
        var language = RootNode.DetectLanguage(text);

        Compilation compilation;

        if (language == Nodes.Language.CSharp)
        {
            compilation = CSharpCompilation.Create(
                $"WebForms_{tempAssemblyName}",
                references: References,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    checkOverflow: true,
                    platform: Platform.AnyCpu
                )
            );
        }
        else
        {
            compilation = VisualBasicCompilation.Create(
                $"WebForms_{tempAssemblyName}",
                references: References,
                options: new VisualBasicCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    checkOverflow: true,
                    platform: Platform.AnyCpu
                )
            );
        }

        var type = RootNode.Parse(compilation, path, text);

        if (type.Inherits == null)
        {
            throw new TypeLoadException($"Could not load type {RootNode.DetectInherits(text)}");
        }

        var assemblyName = type.Inherits.ContainingAssembly.ToDisplayString();
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName) ??
                       Assembly.Load(assemblyName);

        var rootNamespace = assembly.GetCustomAttribute<RootNamespaceAttribute>()?.Namespace;

        compilation = compilation.AddSyntaxTrees(
            type.GenerateCode(rootNamespace)
        );

        return new ViewCompileResult(compilation, type);
    }

    private static IReadOnlyList<MetadataReference> References
    {
        get
        {
            lock (ReferencesLock)
            {
                if (_references != null) return _references;

                var references = new HashSet<MetadataReference>();

                foreach (var referencedAssembly in GetAssemblies())
                {
                    references.Add(MetadataReference.CreateFromFile(referencedAssembly.Location));
                }

                _references = references.ToArray();

                return _references;
            }
        }
    }

    private static List<Assembly> GetAssemblies()
    {
        var returnAssemblies = new List<Assembly>();
        var loadedAssemblies = new HashSet<string>();
        var assembliesToCheck = new Queue<Assembly>();
        var current = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        loadedAssemblies.Add(current.FullName);
        returnAssemblies.Add(current);

        assembliesToCheck.Enqueue(current);

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

#if NETFRAMEWORK
        if (System.Web.HttpRuntime.BinDirectory is { } binDirectory)
        {
            foreach (var dllFile in Directory.GetFiles(binDirectory, "*.dll"))
            {
                var assemblyName = AssemblyName.GetAssemblyName(dllFile);

                if (!loadedAssemblies.Contains(assemblyName.FullName))
                {
                    var assembly = Assembly.Load(assemblyName);
                    loadedAssemblies.Add(assemblyName.FullName);
                    returnAssemblies.Add(assembly);
                }
            }
        }
#endif

        return returnAssemblies;
    }
}
