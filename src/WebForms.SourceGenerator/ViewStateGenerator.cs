using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Scriban;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace WebForms.SourceGenerator
{
    [Generator]
    public class ViewStateGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeDeclaration = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: Predicate,
                    transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node);

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(typeDeclaration.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static bool Predicate(SyntaxNode s, CancellationToken _)
        {
            if (s is not TypeDeclarationSyntax type)
            {
                return false;
            }

            foreach (var member in type.Members)
            {
                if (HasViewStateAttribute(member))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasViewStateAttribute(MemberDeclarationSyntax member)
        {
            foreach (var attributeList in member.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name switch
                    {
                        IdentifierNameSyntax identifier => identifier.Identifier.Text,
                        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                        _ => attribute.Name.ToString()
                    };

                    if (name is "ViewState" or "ViewStateAttribute")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public record ClassItem(
            string? Namespace,
            string Type,
            List<PropertyItem> Properties
        );

        public record PropertyItem(
            int Id,
            string Name,
            string Type
        );

        public record Model(
            List<ClassItem> Items
        );

        public static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> typeDeclarations, SourceProductionContext context)
        {
            const string file = "Templates/viewstate.scriban";

            var items = new List<ClassItem>();

            foreach (var typeDeclaration in typeDeclarations)
            {
                var model = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);

                var properties = new List<PropertyItem>();
                var id = 0;

                foreach (var member in typeDeclaration.Members)
                {
                    if (!HasViewStateAttribute(member))
                    {
                        continue;
                    }

                    var (declarationType, names) = member switch
                    {
                        FieldDeclarationSyntax f => (f.Declaration.Type, f.Declaration.Variables.Select(i => i.Identifier.Text)),
                        PropertyDeclarationSyntax p => (p.Type, new[] { p.Identifier.Text }),
                        _ => default
                    };

                    if (declarationType == null || names == null) continue;

                    var type = model.GetTypeInfo(declarationType).Type;

                    if (type == null) continue;

                    foreach (var name in names)
                    {
                        properties.Add(new PropertyItem(
                            id++,
                            name,
                            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        ));
                    }
                }

                var ns = typeDeclaration.GetNamespace();

                if (string.IsNullOrEmpty(ns))
                {
                    ns = null;
                }

                items.Add(new ClassItem(
                    ns,
                    typeDeclaration.Identifier.Text,
                    properties
                ));
            }

            var templateModel = new Model(
                items
            );
            
            var template = Template.Parse(EmbeddedResource.GetContent(file), file);
            var output = template.Render(templateModel, member => member.Name);

            context.AddSource("WebForms.ViewState.cs", SourceText.From(output, Encoding.UTF8));
        }
    }

}
