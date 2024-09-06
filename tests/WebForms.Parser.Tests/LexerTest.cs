using WebFormsCore.Language;
using WebFormsCore.Models;

namespace WebFormsCore.Parser.Tests;

public class LexerTest
{
    [Theory]
    [InlineData("text", """<div>Test</div>""")]
    [InlineData("control", """<div runat="server"><Inner><span runat="server" /></Inner></div>""")]
    [InlineData("script", "<html><body><script></script></body></html>")]
    [InlineData("include", """<!--#include file="Common/Include.ascx" -->""")]
    [InlineData("img", """<img src="test.png" />""")]
    [InlineData("img-expression", """<img src="<%= Var %>" />""")]
    [InlineData("newline", """
       <fortyfingers:STYLEHELPER ID="favicon" runat="server" AddToHead='
          <link rel="icon" type="image/ico" href="[S]img/favicon.ico" />
       ' AddAtEnd="False" />
       """)]
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
