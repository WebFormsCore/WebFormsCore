using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WebFormsCore.Models;

public readonly record struct TokenRange(string File, TokenPosition Start, TokenPosition End)
{
    public override string ToString()
    {
        return $"{Start} - {End}";
    }

    public bool Includes(int line, int column)
    {
        return (line > Start.Line || line == Start.Line && column >= Start.Column) &&
               (line < End.Line || line == End.Line && column <= End.Column);
    }

    public static implicit operator OffsetRange(TokenRange range)
    {
        return new OffsetRange(range.Start.Offset, range.End.Offset);
    }

    public TokenRange WithEnd(TokenPosition end)
    {
        return this with { End = end };
    }

    public static implicit operator TextSpan(TokenRange range) => new(range.Start.Offset, range.End.Offset - range.Start.Offset);

    public static implicit operator LinePositionSpan(TokenRange range) => new(range.Start, range.End);

    public static implicit operator Location(TokenRange range) => Location.Create(range.File, range, range);

    public TokenRange Slice(int offset)
    {
        return this with
        {
            Start = Start with
            {
                Column = Start.Column + offset,
                Offset = Start.Offset + offset
            }
        };
    }

    public TokenRange Slice(int offset, int length)
    {
        return this with
        {
            Start = Start with
            {
                Column = Start.Column + offset,
                Offset = Start.Offset + offset
            },
            End = Start with
            {
                Column = Start.Column + offset + length,
                Offset = Start.Offset + offset + length
            }
        };
    }
}
