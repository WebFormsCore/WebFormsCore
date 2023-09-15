using Microsoft.CodeAnalysis.Text;

namespace WebFormsCore.Models;

public readonly record struct TokenPosition(int Offset, int Line, int Column)
{
    public override string ToString()
    {
        return $"{Line}:{Column}";
    }

    public string ColumnOffsetString => new(' ', Column);

    public static implicit operator LinePosition(TokenPosition position) => new(position.Line, position.Column);
}
