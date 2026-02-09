using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

[Generator]
public class FromRouteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Predicate(node),
                transform: static (ctx, _) => Transform(ctx))
            .Where(static x => x.Properties.Length > 0);

        var languageVersion = context.CompilationProvider
            .Select(static (c, _) => c is CSharpCompilation cs ? (int)cs.LanguageVersion : 0);

        var compilationAndTypes = typeDeclarations
            .Collect()
            .Combine(languageVersion);

        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static bool Predicate(SyntaxNode node)
    {
        if (node is not TypeDeclarationSyntax type)
        {
            return false;
        }

        foreach (var member in type.Members)
        {
            if (member is PropertyDeclarationSyntax property && HasFromRouteAttribute(property))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFromRouteAttribute(PropertyDeclarationSyntax property)
    {
        foreach (var attributeList in property.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                    _ => attribute.Name.ToString()
                };

                if (name is "FromRoute" or "FromRouteAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? GetFromRouteNameArgument(PropertyDeclarationSyntax property)
    {
        foreach (var attributeList in property.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                    _ => attribute.Name.ToString()
                };

                if (name is "FromRoute" or "FromRouteAttribute")
                {
                    // Check for constructor argument: [FromRoute("paramName")]
                    if (attribute.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals is null)?.Expression is LiteralExpressionSyntax literal)
                    {
                        return literal.Token.ValueText;
                    }

                    // Check for named argument: [FromRoute(Name = "paramName")]
                    if (attribute.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.Text == "Name")?.Expression is LiteralExpressionSyntax namedLiteral)
                    {
                        return namedLiteral.Token.ValueText;
                    }
                }
            }
        }

        return null;
    }

    private readonly record struct RoutePropertyInfo(
        string PropertyName,
        string RouteName,
        string TypeFullName,
        string TypeShortName,
        string AccessModifier,
        bool IsNullable
    );

    private readonly record struct TypeInfo(
        string? Namespace,
        string TypeName,
        EquatableArray<RoutePropertyInfo> Properties
    );

    private static TypeInfo Transform(GeneratorSyntaxContext ctx)
    {
        var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
        var semanticModel = ctx.SemanticModel;
        var properties = ImmutableArray.CreateBuilder<RoutePropertyInfo>();

        foreach (var member in typeDeclaration.Members)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                continue;
            }

            if (!HasFromRouteAttribute(property))
            {
                continue;
            }

            // Must be partial
            if (!property.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                continue;
            }

            // Must not have a body (it's a declaration, not an implementation)
            if (property.AccessorList?.Accessors.Any(a => a.Body != null || a.ExpressionBody != null) == true)
            {
                continue;
            }

            var typeInfo = semanticModel.GetTypeInfo(property.Type);
            var typeSymbol = typeInfo.Type;

            if (typeSymbol == null)
            {
                continue;
            }

            var routeName = GetFromRouteNameArgument(property) ?? property.Identifier.Text;
            var typeFullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Determine the short name for conversion
            var typeShortName = typeSymbol.SpecialType switch
            {
                SpecialType.System_String => "string",
                SpecialType.System_Int32 => "int",
                SpecialType.System_Int64 => "long",
                SpecialType.System_Int16 => "short",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Double => "double",
                SpecialType.System_Single => "float",
                SpecialType.System_Decimal => "decimal",
                _ => typeSymbol.Name
            };

            var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated ||
                             (typeSymbol is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

            // Determine access modifier
            var accessModifier = "public";
            foreach (var modifier in property.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.ProtectedKeyword))
                {
                    accessModifier = property.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))
                        ? "protected internal"
                        : "protected";
                    break;
                }

                if (modifier.IsKind(SyntaxKind.InternalKeyword))
                {
                    accessModifier = "internal";
                    break;
                }

                if (modifier.IsKind(SyntaxKind.PrivateKeyword))
                {
                    accessModifier = "private";
                    break;
                }
            }

            properties.Add(new RoutePropertyInfo(
                PropertyName: property.Identifier.Text,
                RouteName: routeName,
                TypeFullName: typeFullName,
                TypeShortName: typeShortName,
                AccessModifier: accessModifier,
                IsNullable: isNullable
            ));
        }

        var ns = typeDeclaration.GetNamespace();

        return new TypeInfo(
            Namespace: string.IsNullOrEmpty(ns) ? null : ns,
            TypeName: typeDeclaration.Identifier.Text,
            Properties: properties.ToImmutable()
        );
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<TypeInfo> types, int languageVersion)
    {
        if (languageVersion is > 0 and < 1300)
        {
            return;
        }

        foreach (var type in types)
        {
            if (type.Properties.Length == 0)
            {
                continue;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using WebFormsCore.UI.Attributes;");
            sb.AppendLine();

            if (type.Namespace != null)
            {
                sb.Append("namespace ").AppendLine(type.Namespace);
                sb.AppendLine("{");
            }

            sb.Append("partial class ").AppendLine(type.TypeName);
            sb.AppendLine("{");

            // Generate backing fields and partial property implementations
            foreach (var prop in type.Properties)
            {
                var defaultAssignment = prop.TypeShortName == "string"
                    ? " = default!;"
                    : ";";

                sb.Append("    private ").Append(prop.TypeFullName).Append(" _fromRoute_").Append(prop.PropertyName).AppendLine(defaultAssignment);
                sb.AppendLine();
                sb.Append("    ").Append(prop.AccessModifier).Append(" partial ").Append(prop.TypeFullName).Append(' ').AppendLine(prop.PropertyName);
                sb.AppendLine("    {");
                sb.Append("        get => _fromRoute_").Append(prop.PropertyName).AppendLine(";");
                sb.Append("        set => _fromRoute_").Append(prop.PropertyName).AppendLine(" = value;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Generate SetContext override
            sb.AppendLine("    protected internal override void SetContext(Microsoft.AspNetCore.Http.HttpContext context)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.SetContext(context);");
            sb.AppendLine();
            sb.AppendLine("        var routeValues = context.Request.RouteValues;");

            foreach (var prop in type.Properties)
            {
                sb.AppendLine();
                sb.Append("        if (routeValues.TryGetValue(\"").Append(prop.RouteName).Append("\", out var _rv_").Append(prop.PropertyName).AppendLine("))");
                sb.AppendLine("        {");
                GenerateConversion(sb, prop);
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");

            sb.AppendLine("}");

            if (type.Namespace != null)
            {
                sb.AppendLine("}");
            }

            var fileName = type.Namespace != null
                ? $"{type.Namespace}.{type.TypeName}.FromRoute.g.cs"
                : $"{type.TypeName}.FromRoute.g.cs";

            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static void GenerateConversion(StringBuilder sb, RoutePropertyInfo prop)
    {
        var varName = $"_rv_{prop.PropertyName}";

        if (prop.TypeShortName == "string")
        {
            sb.Append("            ").Append(prop.PropertyName).Append(" = ").Append(varName);
            sb.AppendLine(prop.IsNullable ? "?.ToString();" : "?.ToString() ?? default!;");
            return;
        }

        // Use IAttributeParser<T> for all non-string types
        sb.Append("            if (").Append(varName).Append(" is ").Append(prop.TypeFullName).Append(" _typed_").Append(prop.PropertyName).AppendLine(")");
        sb.AppendLine("            {");
        sb.Append("                ").Append(prop.PropertyName).Append(" = _typed_").Append(prop.PropertyName).AppendLine(";");
        sb.AppendLine("            }");
        sb.Append("            else if (").Append(varName).AppendLine(" != null)");
        sb.AppendLine("            {");
        sb.Append("                ").Append(prop.PropertyName).Append(" = context.RequestServices.GetRequiredService<IAttributeParser<").Append(prop.TypeFullName).AppendLine(">>()");
        sb.Append("                    .Parse(").Append(varName).AppendLine(".ToString()!);");
        sb.AppendLine("            }");
    }
}
