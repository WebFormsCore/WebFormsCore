using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

[Generator]
public class BindingSourceGenerator : IIncrementalGenerator
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
            if (member is PropertyDeclarationSyntax property && GetBindingSource(property) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static BindingSource? GetBindingSource(PropertyDeclarationSyntax property)
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

                var source = GetBindingSourceFromName(name);
                if (source != null)
                {
                    return source;
                }
            }
        }

        return null;
    }

    private static BindingSource? GetBindingSourceFromName(string name)
    {
        return name switch
        {
            "FromRoute" or "FromRouteAttribute" => BindingSource.Route,
            "FromQuery" or "FromQueryAttribute" => BindingSource.Query,
            "FromHeader" or "FromHeaderAttribute" => BindingSource.Header,
            "FromServices" or "FromServicesAttribute" => BindingSource.Services,
            _ => null
        };
    }

    private static string? GetNameArgument(PropertyDeclarationSyntax property)
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

                if (GetBindingSourceFromName(name) == null)
                {
                    continue;
                }

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

        return null;
    }

    private enum BindingSource
    {
        Route,
        Query,
        Header,
        Services
    }

    private readonly record struct BindingPropertyInfo(
        string PropertyName,
        string ParameterName,
        string TypeFullName,
        string TypeShortName,
        string AccessModifier,
        bool IsNullable,
        bool IsNullableReferenceType,
        BindingSource Source
    )
    {
        /// <summary>
        /// The type name to use in field and property declarations, including ? for nullable reference types.
        /// </summary>
        public string DeclarationTypeName => IsNullableReferenceType ? TypeFullName + "?" : TypeFullName;
    }

    private readonly record struct TypeInfo(
        string? Namespace,
        string TypeName,
        EquatableArray<BindingPropertyInfo> Properties
    );

    private static TypeInfo Transform(GeneratorSyntaxContext ctx)
    {
        var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
        var semanticModel = ctx.SemanticModel;
        var properties = ImmutableArray.CreateBuilder<BindingPropertyInfo>();

        foreach (var member in typeDeclaration.Members)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                continue;
            }

            var bindingSource = GetBindingSource(property);
            if (bindingSource == null)
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

            var parameterName = GetNameArgument(property) ?? property.Identifier.Text;
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
                             (typeSymbol is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) ||
                             (property.Type is NullableTypeSyntax);

            var isNullableReferenceType = property.Type is NullableTypeSyntax && typeSymbol.IsReferenceType;

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

            properties.Add(new BindingPropertyInfo(
                PropertyName: property.Identifier.Text,
                ParameterName: parameterName,
                TypeFullName: typeFullName,
                TypeShortName: typeShortName,
                AccessModifier: accessModifier,
                IsNullable: isNullable,
                IsNullableReferenceType: isNullableReferenceType,
                Source: bindingSource.Value
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
                var fieldPrefix = GetFieldPrefix(prop.Source);
                var defaultAssignment = prop.TypeShortName == "string"
                    ? " = default!;"
                    : ";";

                sb.Append("    private ").Append(prop.DeclarationTypeName).Append(' ').Append(fieldPrefix).Append(prop.PropertyName).AppendLine(defaultAssignment);
                sb.AppendLine();
                sb.Append("    ").Append(prop.AccessModifier).Append(" partial ").Append(prop.DeclarationTypeName).Append(' ').AppendLine(prop.PropertyName);
                sb.AppendLine("    {");
                sb.Append("        get => ").Append(fieldPrefix).Append(prop.PropertyName).AppendLine(";");
                sb.Append("        set => ").Append(fieldPrefix).Append(prop.PropertyName).AppendLine(" = value;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Generate SetContext override
            sb.AppendLine("    protected internal override void SetContext(Microsoft.AspNetCore.Http.HttpContext context)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.SetContext(context);");

            var hasRouteProperties = false;
            foreach (var prop in type.Properties)
            {
                if (prop.Source == BindingSource.Route)
                {
                    hasRouteProperties = true;
                    break;
                }
            }

            if (hasRouteProperties)
            {
                sb.AppendLine();
                sb.AppendLine("        var routeValues = context.Request.RouteValues;");
            }

            foreach (var prop in type.Properties)
            {
                sb.AppendLine();

                switch (prop.Source)
                {
                    case BindingSource.Route:
                        GenerateRouteBinding(sb, prop);
                        break;
                    case BindingSource.Query:
                        GenerateStringValuesBinding(sb, prop, "context.Request.Query", "_qv_");
                        break;
                    case BindingSource.Header:
                        GenerateStringValuesBinding(sb, prop, "context.Request.Headers", "_hv_");
                        break;
                    case BindingSource.Services:
                        GenerateServicesBinding(sb, prop);
                        break;
                }
            }

            sb.AppendLine("    }");

            sb.AppendLine("}");

            if (type.Namespace != null)
            {
                sb.AppendLine("}");
            }

            var fileName = type.Namespace != null
                ? $"{type.Namespace}.{type.TypeName}.Binding.g.cs"
                : $"{type.TypeName}.Binding.g.cs";

            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string GetFieldPrefix(BindingSource source)
    {
        return source switch
        {
            BindingSource.Route => "_fromRoute_",
            BindingSource.Query => "_fromQuery_",
            BindingSource.Header => "_fromHeader_",
            BindingSource.Services => "_fromServices_",
            _ => "_binding_"
        };
    }

    private static void GenerateRouteBinding(StringBuilder sb, BindingPropertyInfo prop)
    {
        var varName = $"_rv_{prop.PropertyName}";

        sb.Append("        if (routeValues.TryGetValue(\"").Append(prop.ParameterName).Append("\", out var ").Append(varName).AppendLine("))");
        sb.AppendLine("        {");

        if (prop.TypeShortName == "string")
        {
            sb.Append("            ").Append(prop.PropertyName).Append(" = ").Append(varName);
            sb.AppendLine(prop.IsNullable ? "?.ToString();" : "?.ToString() ?? default!;");
        }
        else
        {
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

        sb.AppendLine("        }");
    }

    private static void GenerateStringValuesBinding(StringBuilder sb, BindingPropertyInfo prop, string collectionAccessor, string varPrefix)
    {
        var varName = $"{varPrefix}{prop.PropertyName}";

        sb.Append("        if (").Append(collectionAccessor).Append(".TryGetValue(\"").Append(prop.ParameterName).Append("\", out var ").Append(varName).Append(") && ").Append(varName).AppendLine(".Count > 0)");
        sb.AppendLine("        {");

        if (prop.TypeShortName == "string")
        {
            sb.Append("            ").Append(prop.PropertyName).Append(" = ").Append(varName);
            sb.AppendLine(prop.IsNullable ? ".ToString();" : ".ToString();");
        }
        else
        {
            sb.Append("            ").Append(prop.PropertyName).Append(" = context.RequestServices.GetRequiredService<IAttributeParser<").Append(prop.TypeFullName).AppendLine(">>()");
            sb.Append("                .Parse(").Append(varName).AppendLine(".ToString());");
        }

        sb.AppendLine("        }");
    }

    private static void GenerateServicesBinding(StringBuilder sb, BindingPropertyInfo prop)
    {
        if (prop.IsNullable)
        {
            sb.Append("        ").Append(prop.PropertyName).Append(" = context.RequestServices.GetService<").Append(prop.TypeFullName).AppendLine(">();");
        }
        else
        {
            sb.Append("        ").Append(prop.PropertyName).Append(" = context.RequestServices.GetRequiredService<").Append(prop.TypeFullName).AppendLine(">();");
        }
    }
}
