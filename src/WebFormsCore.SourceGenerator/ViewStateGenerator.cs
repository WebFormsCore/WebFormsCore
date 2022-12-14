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
            List<PropertyItem> Properties,
            string FlagType
        );

        public record PropertyItem(
            int Id,
            string Name,
            string Type,
            string? ValidateProperty,
            string? DefaultValue,
            int Flag
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
                var flag = 1;

                foreach (var member in typeDeclaration.Members)
                {
                    if (!HasViewStateAttribute(member, out var validateProperty))
                    {
                        continue;
                    }

                    if (member is FieldDeclarationSyntax field)
                    {
                        var type = model.GetTypeInfo(field.Declaration.Type).Type;

                        if (type == null) continue;

                        foreach (var variable in field.Declaration.Variables)
                        {
                            properties.Add(new PropertyItem(
                                id++,
                                variable.Identifier.Text,
                                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                validateProperty,
                                variable.Initializer?.Value.ToString(),
                                flag
                            ));

                            flag *= 2;
                        }
                    }
                    else if (member is PropertyDeclarationSyntax property)
                    {
                        var type = model.GetTypeInfo(property.Type).Type;

                        if (type == null) continue;

                        properties.Add(new PropertyItem(
                            id++,
                            property.Identifier.Text,
                            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            validateProperty,
                            property.Initializer?.Value.ToString(),
                            flag
                        ));

                        flag *= 2;
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
                    properties,
                    properties.Count switch
                    {
                        <= 8 => "byte",
                        <= 16 => "ushort",
                        <= 32 => "uint",
                        _ => "ulong"
                    }
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
