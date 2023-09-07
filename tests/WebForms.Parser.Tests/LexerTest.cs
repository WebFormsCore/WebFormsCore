using WebFormsCore.Language;
using WebFormsCore.Models;

namespace WebFormsCore.Parser.Tests;

[UsesVerify]
public class LexerTest
{
    [Theory]
    [InlineData("text", """<div>Test</div>""")]
    [InlineData("control", """<div runat="server"><Inner><span runat="server" /></Inner></div>""")]
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
