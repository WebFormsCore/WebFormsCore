﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using WebFormsCore.Nodes;

namespace WebFormsCore.Compiler;

public record struct ViewCompileResult(Compilation Compilation, RootNode Type);

public static class ViewCompiler
{
    private static IReadOnlyList<MetadataReference>? _references;
    private static readonly object ReferencesLock = new();

    public static ViewCompileResult Compile(string path, string? text = null, string? rootDirectory = null, bool generateHash = true, bool concurrentBuild = true, IReadOnlyList<MetadataReference>? references = null)
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
                references: references ?? References,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    checkOverflow: true,
                    concurrentBuild: concurrentBuild
                )
            );
        }
        else
        {
            compilation = VisualBasicCompilation.Create(
                $"WebForms_{tempAssemblyName}",
                references: references ?? References,
                options: new VisualBasicCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    checkOverflow: true,
                    concurrentBuild: concurrentBuild
                )
            );
        }

        var type = RootNode.Parse(
            out _,
            compilation,
            path,
            text,
            namespaces: GetNamespaces(path),
            addFields: false,
            rootDirectory: rootDirectory,
            generateHash: generateHash
        );

        string? rootNamespace = null;

        if (type.Inherits != null)
        {
            var assemblyName = type.Inherits.ContainingAssembly.ToDisplayString();
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName) ??
                           Assembly.Load(assemblyName);

            rootNamespace = assembly.GetCustomAttribute<RootNamespaceAttribute>()?.Namespace;
        }

        var code = type.GenerateCode(rootNamespace);

        compilation = compilation.AddSyntaxTrees(code);

        return new ViewCompileResult(compilation, type);
    }

    private static IEnumerable<KeyValuePair<string, string>>? GetNamespaces(string path)
    {
        IEnumerable<KeyValuePair<string, string>>? namespaces = null;
        var parentDirectory = Path.GetDirectoryName(path);

        while (parentDirectory != null)
        {
            var webConfigPath = Path.Combine(parentDirectory, "web.config");

            if (File.Exists(webConfigPath))
            {
                try
                {
                    namespaces = RootNode.GetNamespaces(
                        File.ReadAllText(webConfigPath)
                    );
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            parentDirectory = Path.GetDirectoryName(parentDirectory);
        }

        return namespaces;
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
                    if (referencedAssembly.IsDynamic || string.IsNullOrEmpty(referencedAssembly.Location)) continue;

                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(referencedAssembly.Location));
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
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

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            loadedAssemblies.Add(assembly.FullName!);
            returnAssemblies.Add(assembly);
            assembliesToCheck.Enqueue(assembly);
        }

        while(assembliesToCheck.Any())
        {
            var assemblyToCheck = assembliesToCheck.Dequeue();

            foreach(var reference in assemblyToCheck.GetReferencedAssemblies())
            {
                if (loadedAssemblies.Contains(reference.FullName))
                {
                    continue;
                }

                var assembly = TryLoad(reference);

                if (assembly == null)
                {
                    continue;
                }

                assembliesToCheck.Enqueue(assembly);
                loadedAssemblies.Add(reference.FullName);
                returnAssemblies.Add(assembly);
            }
        }

        return returnAssemblies;
    }

    private static Assembly? TryLoad(AssemblyName reference)
    {
        try
        {
            return Assembly.Load(reference);
        }
        catch
        {
            return null;
        }
    }
}
