using Microsoft.CodeAnalysis;

namespace WebFormsCore;

public record MemberResult(string Name, ITypeSymbol Type, bool CanWrite, ISymbol Symbol)
{
    public string DisplayType => Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}