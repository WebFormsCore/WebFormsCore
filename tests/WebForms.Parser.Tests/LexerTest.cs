using WebFormsCore.Language;
using WebFormsCore.Models;

namespace WebForms.Parser.Tests;

[UsesVerify]
public class LexerTest
{
    [Theory]
    [InlineData("text", """<div>Test</div>""")]
    [InlineData("control", """<div runat="server">Test</div>""")]
    public Task TestLexer(string name, string input)
    {
        var lexer = new Lexer("Tests.aspx", input);
        var output = new List<Token>();

        while (lexer.Next() is { } next)
        {
            output.Add(next);
        }

        return Verify(output)
            .UseParameters(name);
    }
}
