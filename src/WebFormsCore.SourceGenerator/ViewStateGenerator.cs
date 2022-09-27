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

        private static bool Predicate(SyntaxNode s, CancellationToken token)
        {
            if (s is not TypeDeclarationSyntax type)
            {
                return false;
            }

            foreach (var member in type.Members)
            {
                if (HasViewStateAttribute(member, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasViewStateAttribute(MemberDeclarationSyntax member, out string? validateProperty)
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
                        validateProperty = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression switch
                        {
                            LiteralExpressionSyntax literal => literal.Token.ValueText,
                            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression switch
                            {
                                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                                _ => null
                            },
                            _ => null
                        };
                        return true;
                    }
                }
            }

            validateProperty = null;
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
            string Type,
            string? ValidateProperty
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
                    if (!HasViewStateAttribute(member, out var validateProperty))
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
                            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            validateProperty
                        ));
                    }
                }

                var ns = typeDeclaration.GetNamespace();

                if (string.IsNullOrEmpty(ns))
                {
                    ns = null;
                }

                var typeName = typeDeclaration.Identifier.Text;

                if (typeDeclaration.TypeParameterList is { } typeParameterList)
                {
                    typeName += typeParameterList.ToString();
                }

                items.Add(new ClassItem(
                    ns,
                    typeName,
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
