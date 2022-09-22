namespace WebForms.Models;

public record struct Token(TokenType Type, TokenRange Range, TokenString Text)
{
    public override string ToString()
    {
        return Text.Value;
    }
}
