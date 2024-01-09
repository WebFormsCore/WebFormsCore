using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;

namespace WebFormsCore.SourceGenerator
{
    [Generator]
    public class ControlRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeDeclaration = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 },
                    transform: static (ctx, token) =>
                    {
                        var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;

                        if (ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration, token) is not INamedTypeSymbol type ||
                            type is { IsAbstract: true } or { IsGenericType: true } or { DeclaredAccessibility: Accessibility.Private})
                        {
                            return null;
                        }

                        // Check if the base type is a control
                        var baseType = type.BaseType;

                        while (baseType is not null)
                        {
                            if (baseType.Name == "Control" && baseType.ContainingNamespace.ToString() == "WebFormsCore.UI")
                            {
                                var name = typeDeclaration.Identifier.Text;
                                var ns = type!.ContainingNamespace.ToString();
                                return $"{ns}.{name}";
                            }

                            baseType = baseType.BaseType;
                        }

                        return null;
                    });

            var rootNamespace = context.AnalyzerConfigOptionsProvider
                .Select((options, _) => options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns) ? ns : null);

            var assemblyName = context.CompilationProvider
                .Select((compilation, _) => compilation.AssemblyName);

            var compilationAndClasses
                = typeDeclaration.Collect()
                    .Combine(rootNamespace.Combine(assemblyName))
                    .Select((i, _) => (Types: i.Left, RootNamespace: i.Right.Left ?? i.Right.Right));

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Types, source.RootNamespace, spc));
        }

        private static void Execute(ImmutableArray<string?> namespaces, string? rootNamespace, SourceProductionContext spc)
        {
            var sb = new StringBuilder();

            if (rootNamespace is not null)
            {
                sb.AppendLine($"[assembly: WebFormsCore.RootNamespaceAttribute(\"{rootNamespace}\")]");
            }

            foreach (var ns in namespaces)
            {
                if (ns is not null)
                {
                    sb.AppendLine($"[assembly: WebFormsCore.AssemblyControlAttribute(typeof({ns}))]");
                }
            }

            if (sb.Length > 0)
            {
                spc.AddSource("WebForms.Controls.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }
    }
}
