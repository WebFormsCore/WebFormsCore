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
                    transform: static (ctx, _) =>
                    {
                        var items = GetItems((TypeDeclarationSyntax)ctx.Node, ctx.SemanticModel.Compilation.GetSemanticModel(ctx.Node.SyntaxTree));
                        var (ns, name) = ctx.Node switch
                        {
                            ClassDeclarationSyntax classDeclaration => (GetNamespace(classDeclaration), classDeclaration.Identifier.Text),
                            StructDeclarationSyntax structDeclaration => (GetNamespace(structDeclaration), structDeclaration.Identifier.Text),
                            _ => (null, "ViewState")
                        };

                        return string.IsNullOrEmpty(ns)
                            ? (name, items)
                            : ($"{ns}.{name}", items);
                    });

            context.RegisterSourceOutput(typeDeclaration, Execute);
        }

        public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            string nameSpace = string.Empty;
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            while (potentialNamespaceParent != null &&
                   potentialNamespaceParent is not NamespaceDeclarationSyntax
                   && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                nameSpace = namespaceParent.Name.ToString();

                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            return nameSpace;
        }

        private static bool Predicate(SyntaxNode s, CancellationToken token)
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

        private static bool HasViewStateAttribute(MemberDeclarationSyntax member, out string? validateProperty, out bool trackDefault)
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
                        trackDefault = attribute.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.Text == "WriteAlways")?.Expression switch
                        {
                            LiteralExpressionSyntax literal => (bool?)literal.Token.Value ?? false,
                            _ => false
                        };

                        validateProperty = attribute.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals is null)?.Expression switch
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
            trackDefault = false;
            return false;
        }

        public record ClassItem(
            string? Namespace,
            string Type,
            ImmutableArray<PropertyItem> Properties,
            string FlagType,
            bool IsControl
        );

        public record PropertyItem(
            int Id,
            string Name,
            string Type,
            string? ValidateProperty,
            string? DefaultValue,
            int Flag,
            bool IsViewStateObject,
            bool TrackDefault
        );

        public record Model(
            IEnumerable<ClassItem> Items
        );


        private void Execute(SourceProductionContext context, (string, ImmutableArray<ClassItem>) tuple)
        {
            const string file = "Templates/viewstate.scriban";

            var (name, items) = tuple;
            var templateModel = new Model(items);

            var template = Template.Parse(EmbeddedResource.GetContent(file), file);
            var output = template.Render(templateModel, member => member.Name);

            context.AddSource(name.Replace('.', '_'), SourceText.From(output, Encoding.UTF8));
        }

        private static ImmutableArray<ClassItem> GetItems(TypeDeclarationSyntax typeDeclaration, SemanticModel model)
        {
            var properties = new List<PropertyItem>();
            var id = 0;
            var flag = 1;

            foreach (var member in typeDeclaration.Members)
            {
                if (!HasViewStateAttribute(member, out var validateProperty, out var trackDefault))
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
                            flag,
                            type.AllInterfaces.Any(x => x.Name == "IViewStateObject"),
                            trackDefault
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
                        flag,
                        type.AllInterfaces.Any(x => x.Name == "IViewStateObject"),
                        trackDefault
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

            var symbol = model.GetDeclaredSymbol(typeDeclaration) as ITypeSymbol;
            var isControl = false;

            var baseType = symbol?.BaseType;

            while (baseType != null)
            {
                if (baseType.Name == "Control")
                {
                    isControl = true;
                    break;
                }

                baseType = baseType.BaseType;
            }

            return ImmutableArray.Create(new ClassItem(
                ns,
                typeName,
                properties.ToImmutableArray(),
                properties.Count switch
                {
                    <= 8 => "byte",
                    <= 16 => "ushort",
                    <= 32 => "uint",
                    _ => "ulong"
                },
                isControl
            ));
        }

        public static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> typeDeclarations, SourceProductionContext context)
        {

            var items = new List<ClassItem>();

            foreach (var typeDeclaration in typeDeclarations)
            {
                var model = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);


            }
        }
    }

}
