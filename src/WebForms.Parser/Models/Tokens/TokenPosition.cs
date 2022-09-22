using Microsoft.CodeAnalysis.Text;

namespace WebForms.Models;

public readonly record struct TokenPosition(int Offset, int Line, int Column)
{
    public override string ToString()
    {
        return $"{Line}:{Column}";
    }

    public static implicit operator LinePosition(TokenPosition position) => new(position.Line, position.Column);
}
