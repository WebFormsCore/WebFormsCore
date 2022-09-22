using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WebForms.Models;
using WebForms.Nodes;

namespace WebForms;

public class Parser
{
    public static readonly CSharpParseOptions StatementOptions = new(kind: SourceCodeKind.Script);
    private readonly ParserContainer _rootContainer = new();
    private ParserContainer _container;
    private ParserContainer? _headerContainer;
    private string? _itemType;

    public Parser()
    {
        _container = _rootContainer;
    }

    public RootNode Root => _container.Root;

    public List<Diagnostic> Diagnostics { get; } = new();

    public void Parse(ref Lexer lexer)
    {
        while (lexer.Next() is { } token)
        {
            Consume(ref lexer, token);
        }
    }

    private void Consume(ref Lexer lexer, Token token)
    {
        switch (token.Type)
        {
            case TokenType.Expression:
                ConsumeExpression(token, false);
                break;
            case TokenType.EvalExpression:
                ConsumeExpression(token, true);
                break;
            case TokenType.Statement:
                ConsumeStatement(token);
                break;
            case TokenType.TagOpen:
                ConsumeOpenTag(ref lexer, token.Range.Start);
                break;
            case TokenType.TagOpenSlash:
                ConsumeCloseTag(ref lexer, token.Range.Start);
                break;
            case TokenType.StartDirective:
                ConsumeDirective(ref lexer, token.Range.Start);
                break;
            case TokenType.Text:
                ConsumeText(token);
                break;
        }
    }

    private void ConsumeText(Token token)
    {
        _container.AddText(new TextNode
        {
            Range = token.Range,
            Text = token.Text
        });
    }

    private void ConsumeExpression(Token token, bool isEval)
    {
        var element = new ExpressionNode
        {
            Range = token.Range,
            Text = token.Text,
            IsEval = isEval,
            ItemType = isEval ? _itemType : null
        };

        _container.AddExpression(element);
    }

    private void ConsumeStatement(Token token)
    {
        var element = new StatementNode
        {
            Range = token.Range,
            Text = token.Text
        };

        _container.AddStatement(element);
    }

    private void ConsumeDirective(ref Lexer lexer, TokenPosition startPosition)
    {
        var element = new DirectiveNode
        {
            Range = new TokenRange(lexer.File, startPosition, startPosition)
        };

        var isFirst = true;

        while (lexer.Next() is { } next)
        {
            if (next.Type == TokenType.Attribute)
            {
                TokenString value = default;

                if (lexer.Peek() is { Type: TokenType.AttributeValue } valueNode)
                {
                    lexer.Next();
                    value = valueNode.Text;
                }

                if (isFirst)
                {
                    element.DirectiveType = Enum.TryParse<DirectiveType>(next.Text, true, out var type) ? type : DirectiveType.Unknown;
                    isFirst = false;
                }
                else
                {
                    element.Attributes.Add(next.Text, value);
                }
            }
            else if (next.Type == TokenType.EndDirective)
            {
                element.Range = element.Range.WithEnd(next.Range.End);
                _container.AddDirective(element);
                break;
            }
            else
            {
                Consume(ref lexer, next);
            }
        }
    }

    private void ConsumeOpenTag(ref Lexer lexer, TokenPosition startPosition)
    {
        var element = new HtmlNode
        {
            Range = new TokenRange(lexer.File, startPosition, startPosition)
        };

        if (lexer.Peek() is { Type: TokenType.ElementNamespace } ns)
        {
            element.StartTag.Namespace = ns.Text;
            lexer.Next();
        }

        if (lexer.Peek() is not { Type: TokenType.ElementName } name)
        {
            return;
        }

        lexer.Next();
        element.StartTag.Name = name.Text;
        _container.Push(element);

        while (lexer.Next() is { } next)
        {
            if (next.Type == TokenType.Attribute)
            {
                TokenString value = default;

                if (lexer.Peek() is { Type: TokenType.AttributeValue } valueNode)
                {
                    lexer.Next();
                    value = valueNode.Text;
                }

                if (next.Text.Value.Equals("runat", StringComparison.OrdinalIgnoreCase) &&
                    value.Value.Equals("server", StringComparison.OrdinalIgnoreCase))
                {
                    element.RunAt = RunAt.Server;
                }
                else
                {
                    if (next.Text.Value.Equals("itemtype", StringComparison.OrdinalIgnoreCase))
                    {
                        _itemType = value.Value;
                    }

                    element.Attributes.Add(next.Text, value);
                }
            }
            else if (next.Type == TokenType.TagSlashClose)
            {
                element.Range = element.Range.WithEnd(next.Range.End);
                _container.Pop();
                break;
            }
            else if (next.Type == TokenType.TagClose)
            {
                element.Range = element.Range.WithEnd(next.Range.End);
                break;
            }
            else
            {
                Consume(ref lexer, next);
            }
        }

        element.StartTag.Range = element.Range;

        switch (name.Text.Value)
        {
            case "HeaderTemplate":
                _container = _headerContainer = new ParserContainer(_rootContainer);
                break;
            case "FooterTemplate" when _headerContainer is not null:
                _container = _headerContainer;
                break;
            case "FooterTemplate":
                Diagnostics.Add(Diagnostic.Create(
                    new DiagnosticDescriptor("ASP0001", "FooterTemplate without HeaderTemplate", "FooterTemplate without HeaderTemplate", "ASP", DiagnosticSeverity.Error, true),
                    element.Range));
                break;
        }
    }

    private void ConsumeCloseTag(ref Lexer lexer, TokenPosition startPosition)
    {
        TokenString? endNamespace = null;

        if (lexer.Peek() is {Type: TokenType.ElementNamespace} ns)
        {
            endNamespace = ns.Text;
            lexer.Next();
        }

        if (lexer.Peek() is not {Type: TokenType.ElementName} name)
        {
            return;
        }

        if (name.Text.Value is "HeaderTemplate" or "FooterTemplate")
        {
            _container = _rootContainer;
        }

        var endPosition = name.Range.End;
        lexer.Next();

        if (lexer.Peek() is {Type: TokenType.ElementName} end)
        {
            endPosition = end.Range.End;
            lexer.Next();
        }

        if (lexer.Peek() is { Type: TokenType.TagClose })
        {
            lexer.Next();
        }

        var pop = _container.Pop();

        if (pop == null)
        {
            return;
        }

        if (!pop.Name.Value.Equals(name.Text.Value, StringComparison.OrdinalIgnoreCase) ||
            pop.Namespace.HasValue != endNamespace.HasValue ||
            pop.Namespace.HasValue && !pop.Namespace.Value.Value.Equals(endNamespace?.Value, StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor("ASP0002", "Mismatched closing tag", "Mismatched closing tag", "ASP", DiagnosticSeverity.Error, true),
                    new TokenRange(lexer.File, startPosition, endPosition)));

            return;
        }
        
        pop.Range = pop.Range.WithEnd(endPosition);

        pop.EndTag = new HtmlTagNode
        {
            Name = name.Text,
            Namespace = endNamespace,
            Range = new TokenRange(lexer.File, startPosition, lexer.Position)
        };
    }
}
