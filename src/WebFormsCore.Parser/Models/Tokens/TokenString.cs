using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace WebFormsCore.Models;

public record struct AttributeValue(bool IsCode, TokenString Token)
{
    public override string ToString()
    {
        return Token.ToString();
    }

    public string Value => Token.Value;

    public TokenRange Range => Token.Range;

    public string CodeString => IsCode ? Value : Token.CodeString;

    public string VbCodeString => IsCode ? Value : Token.VbCodeString;

    public static implicit operator string(AttributeValue attributeValue)
    {
        return attributeValue.Value;
    }
}

[DebuggerDisplay("{Value} [{Range}]")]
public readonly struct TokenString : IEquatable<TokenString>
{
    private readonly string _value;

    public TokenString(string value, TokenRange range)
    {
        _value = value;
        Range = range;
    }
    
    public string Value => _value ?? "";

    private string CodeStringBase => Value.Replace("\"", "\"\"");

    public string CodeString => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(Value)).ToFullString();

    public string VbCodeString => @$"""{CodeStringBase.Replace("\r\n", "\" + vbCrLf + \"").Replace("\n", "\" + vbLf + \"").Replace("\r", "\" + vbCr + \"")}""";

    public TokenRange Range { get; }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(TokenString tokenString)
    {
        return tokenString.Value;
    }

    public static implicit operator TokenString(string nodeString)
    {
        return new TokenString(nodeString, default);
    }

    public bool Equals(TokenString other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TokenString other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TokenString left, TokenString right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TokenString left, TokenString right)
    {
        return !left.Equals(right);
    }
}
