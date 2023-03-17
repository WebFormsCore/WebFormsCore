using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Scriban;
using System.Collections.Immutable;
using System.Text;
using WebFormsCore;

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
                    transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node);

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(typeDeclaration.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        public static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> typeDeclarations, SourceProductionContext context)
        {
            var sb = new StringBuilder();

            foreach (var typeDeclaration in typeDeclarations)
            {
                var model = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
                var baseType = model.GetTypeInfo(typeDeclaration.BaseList!.Types[0].Type, context.CancellationToken).Type;
                var isControl = false;

                while (baseType is not null)
                {
                    if (baseType.Name == "Control" && baseType.ContainingNamespace.ToString() == "WebFormsCore.UI")
                    {
                        isControl = true;
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                if (isControl)
                {
                    var type = model.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);
                    var name = typeDeclaration.Identifier.Text;
                    var ns = type!.ContainingNamespace.ToString();
                    var fullName = $"{ns}.{name}";

                    sb.AppendLine($@"[assembly: WebFormsCore.AssemblyControlAttribute(typeof({fullName}))]");
                }
            }

            if (sb.Length > 0)
            {
                context.AddSource("WebForms.Controls.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }
    }

}
