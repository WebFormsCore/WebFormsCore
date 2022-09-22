using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebFormsCore;

public static class ContextExtensions
{
    public static INamedTypeSymbol? GetType(this Compilation context, string ns, string typeName)
    {
        var type = context.GetTypeByMetadataName($"{ns}.{typeName}");

        if (type != null) return type;

        var parts = ns.Split('.');
        var current = context.GlobalNamespace;

        foreach (var part in parts)
        {
            current = current.GetNamespaceMembers()
                .FirstOrDefault(i => i.Name.Equals(part, StringComparison.OrdinalIgnoreCase));

            if (current == null)
            {
                break;
            }
        }

        if (current != null)
        {
            type = current.GetTypeMembers()
                .FirstOrDefault(i => i.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        return type;
    }

    public record MemberResult(string Name, ITypeSymbol Type, bool CanWrite);

    public static MemberResult? GetMemberDeep(this ITypeSymbol type, string name)
    {
        foreach (var symbol in type.GetMembers())
        {
            if (symbol is IPropertySymbol property)
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return new MemberResult(property.Name, property.Type, property.SetMethod != null);
                }
            }
            else if (symbol is IFieldSymbol field)
            {
                if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return new MemberResult(field.Name, field.Type, !field.IsReadOnly);
                }
            }
        }

        if (type.BaseType != null)
        {
            return GetMemberDeep(type.BaseType, name);
        }

        return null;
    }

    public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
    {
        // If we don't have a namespace at all we'll return an empty string
        // This accounts for the "default namespace" case
        string nameSpace = string.Empty;

        // Get the containing syntax node for the type declaration
        // (could be a nested type, for example)
        SyntaxNode? potentialNamespaceParent = syntax.Parent;

        // Keep moving "out" of nested classes etc until we get to a namespace
        // or until we run out of parents
        while (potentialNamespaceParent != null &&
               potentialNamespaceParent is not NamespaceDeclarationSyntax
               && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        // Build up the final namespace by looping until we no longer have a namespace declaration
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // We have a namespace. Use that as the type
            nameSpace = namespaceParent.Name.ToString();

            // Keep moving "out" of the namespace declarations until we
            // run out of nested namespace declarations
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                // Add the outer namespace as a prefix to the final namespace
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        // return the final namespace
        return nameSpace;
    }
}
