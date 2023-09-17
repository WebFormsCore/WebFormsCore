using Microsoft.CodeAnalysis;

namespace WebFormsCore.Nodes;

public record ControlId(bool Designer, string Name, INamedTypeSymbol Type, MemberResult? Member)
{
    public string DisplayType => Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}