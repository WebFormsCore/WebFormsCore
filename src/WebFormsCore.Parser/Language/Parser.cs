using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;
using WebFormsCore.Nodes;

namespace WebFormsCore.Language;

public class Parser
{
    private readonly Compilation _compilation;
    private readonly string? _rootNamespace;
    public static readonly CSharpParseOptions StatementOptions = new(kind: SourceCodeKind.Script);
    private readonly ParserContainer _rootContainer = new();
    private ParserContainer _container;
    private string? _itemType;
    private readonly Dictionary<string, List<string>> _namespaces = new(StringComparer.OrdinalIgnoreCase);
    private INamedTypeSymbol? _type;

    public Parser(Compilation compilation, string? rootNamespace)
    {
        _compilation = compilation;
        _rootNamespace = rootNamespace;
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
            case TokenType.DocType:
                ConsumeDocType(token);
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

    private void ConsumeDocType(Token token)
    {
        _container.AddText(new TextNode
        {
            Range = token.Range,
            Text = new TokenString($"<!DOCTYPE{token.Text}>", token.Range)
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

        if (element.DirectiveType is DirectiveType.Control or DirectiveType.Page)
        {
            if (element.Attributes.TryGetValue("language", out var languageStr))
            {
                Root.Language = languageStr.Value.Equals("VB", StringComparison.OrdinalIgnoreCase)
                    ? Nodes.Language.VisualBasic
                    : Nodes.Language.CSharp;
            }

            if (element.Attributes.TryGetValue("inherits", out var inherits))
            {
                _type = _compilation.GetType(inherits.Value);

                if (_type != null)
                {
                    Root.Inherits = _type;
                }

                if (_type != null && !_type.ContainingNamespace.IsGlobalNamespace)
                {
                    var classNamespace = _type.ContainingNamespace.ToDisplayString();

                    Root.Namespace = classNamespace;

                    if (_rootNamespace != null && classNamespace.StartsWith(_rootNamespace, StringComparison.OrdinalIgnoreCase))
                    {
                        classNamespace = classNamespace.Substring(_rootNamespace.Length).TrimStart('.');

                        if (string.IsNullOrWhiteSpace(classNamespace))
                        {
                            classNamespace = null;
                        }
                    }

                    Root.VbNamespace = classNamespace;
                }
            }
        }

        if (element.DirectiveType is DirectiveType.Register &&
            element.Attributes.TryGetValue("tagprefix", out var tagPrefix) &&
            element.Attributes.TryGetValue("namespace", out var ns))
        {
            AddNamespace(tagPrefix, ns);
        }
    }

    public void AddNamespace(string tagPrefix, string ns)
    {
        if (!_namespaces.TryGetValue(tagPrefix, out var list))
        {
            list = new List<string>();
            _namespaces.Add(tagPrefix, list);
        }

        list.Add(ns);
    }

    private void ConsumeOpenTag(ref Lexer lexer, TokenPosition startPosition)
    {
        Token? ns = null;

        if (lexer.Peek() is { Type: TokenType.ElementNamespace })
        {
            ns = lexer.Next();
        }

        if (lexer.Peek() is not { Type: TokenType.ElementName } name)
        {
            return;
        }

        lexer.Next();
        var runAt = FindRunAt(ref lexer);
        var (selfClosing, attributes) = ConsumeAttributes(ref lexer);

        ElementNode node;

        if (!ns.HasValue &&
            _container.Current is ControlNode parentControl &&
            parentControl.ControlType.GetMemberDeep(name.Text) is {} elementMember
            && elementMember.Type.IsTemplate())
        {
            var templateNode = new TemplateNode
            {
                Property = name,
                ClassName = $"Template_{_type?.Name}_{_container.Current.VariableName}_{name}"
            };

            parentControl.Templates.Add(templateNode);
            Root.Templates.Add(templateNode);

            node = templateNode;
        }
        else if (runAt == RunAt.Server)
        {
            INamedTypeSymbol? controlType = null;

            if (attributes.TryGetValue("itemtype", out var itemTypeStr) &&
                _compilation.GetType(itemTypeStr.Value) is { } itemType)
            {
                var type = GetControlType(ns?.Text, name.Text + "`1", true);

                if (type != null)
                {
                    controlType = type.Construct(itemType);
                }
            }

            controlType ??= GetControlType(ns?.Text, name.Text);
            controlType ??= _compilation.GetType("WebFormsCore.UI.HtmlGenericControl");

            if (controlType == null)
            {
                return;
            }

            var controlNode = new ControlNode(controlType);

            if (attributes.TryGetValue("id", out var id))
            {
                var member = _type?.GetMemberDeep(id.Value);

                controlNode.FieldName = member?.Name ?? id;

                if (_container.Template == null)
                {
                    Root.Ids.Add(new ControlId(id, controlType, member));
                }
            }

            if (controlType.Name == "HtmlGenericControl")
            {
                var member = controlType.GetMemberDeep("TagName");

                if (member != null)
                {
                    controlNode.Properties.Add(new PropertyNode(member, name.Text, null));
                }
            }

            foreach (var attribute in attributes)
            {
                var key = attribute.Key.Value;

                if (key.Equals("runat", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = attribute.Value.Value;
                var range = attribute.Key.Range.WithEnd(attribute.Value.Range.End);

                if (key.StartsWith("On", StringComparison.OrdinalIgnoreCase))
                {
                    var eventSymbol = controlType?.GetDeep<IEventSymbol>(key.Substring(2));
                    var method = _type?.GetDeep<IMethodSymbol>(value);

                    if (eventSymbol != null && method != null)
                    {
                        controlNode.Events.Add(new EventNode(eventSymbol, method)
                        {
                            Range = range
                        });
                        continue;
                    }
                }

                if (controlType?.GetMemberDeep(key) is { CanWrite: true } member)
                {
                    var converterArgument = member.Symbol.GetAttributes()
                        .FirstOrDefault(i => i.AttributeClass.IsAssignableTo("TypeConverterAttribute"))
                        ?.ConstructorArguments[0];

                    var converter = converterArgument?.Value switch
                    {
                        INamedTypeSymbol t => t,
                        string s => _compilation.GetType(s),
                        _ => null
                    };

                    controlNode.Properties.Add(new PropertyNode(member, attribute.Value, converter)
                    {
                        Range = range
                    });
                    continue;
                }

                controlNode.Attributes.Add(attribute.Key, attribute.Value);
            }

            node = controlNode;
        }
        else
        {
            node = new ElementNode
            {
                Attributes = attributes
            };
        }

        node.VariableName = $"ctrl{_container.ControlId++}";
        node.StartTag =  new HtmlTagNode
        {
            Name = name.Text,
            Namespace = ns?.Text,
            Range = new TokenRange(lexer.File, startPosition, lexer.Position)
        };

        node.Range = name.Range;
        _container.Push(node);

        if (selfClosing)
        {
            _container.Pop();
        }
    }

    private static RunAt FindRunAt(ref Lexer lexer)
    {
        var offset = 0;
        var runAt = RunAt.Client;

        while (lexer.Peek(offset) is { } current)
        {
            offset++;

            if (current.Type is TokenType.TagClose or TokenType.TagSlashClose)
            {
                break;
            }

            if (current.Type != TokenType.Attribute ||
                !current.Text.Value.Equals("runat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (lexer.Peek(offset) is { Type: TokenType.AttributeValue } value)
            {
                runAt = value.Text.Value.Equals("server", StringComparison.OrdinalIgnoreCase)
                    ? RunAt.Server
                    : RunAt.Client;
            }

            break;
        }

        return runAt;
    }

    private (bool Closed, Dictionary<TokenString, TokenString> Attributes) ConsumeAttributes(ref Lexer lexer)
    {
        var attributes = new Dictionary<TokenString, TokenString>(AttributeCompare.IgnoreCase);

        while (lexer.Next() is { } keyNode)
        {
            if (keyNode.Type == TokenType.Attribute)
            {
                TokenString value = default;

                if (lexer.Peek() is { Type: TokenType.AttributeValue } valueNode)
                {
                    lexer.Next();
                    value = valueNode.Text;
                }

                var key = keyNode.Text;

                if (key.Value.Equals("itemtype", StringComparison.OrdinalIgnoreCase))
                {
                    _itemType = value.Value;
                }

                attributes.Add(key, value);
            }
            else if (keyNode.Type == TokenType.TagSlashClose)
            {
                return (true, attributes);
            }
            else if (keyNode.Type == TokenType.TagClose)
            {
                return (false, attributes);
            }
            else
            {
                Consume(ref lexer, keyNode);
            }
        }

        return (true, attributes);
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

        pop.EndTag = new HtmlTagNode
        {
            Name = name.Text,
            Namespace = endNamespace,
            Range = new TokenRange(lexer.File, startPosition, lexer.Position)
        };
    }

    private INamedTypeSymbol? GetControlType(TokenString? elementNs, TokenString name, bool returnNull = false)
    {
        if (!elementNs.HasValue)
        {
            return name.Value.ToUpperInvariant() switch
            {
                "FORM" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlForm"),
                "BODY" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlBody"),
                _ => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlGenericControl")
            };
        }

        INamedTypeSymbol? type;

        if (_namespaces.TryGetValue(elementNs, out var list))
        {
            foreach (var ns in list)
            {
                type = _compilation.GetType(ns, name.Value);

                if (type != null)
                {
                    return type;
                }
            }
        }

        if (returnNull)
        {
            return null;
        }

        type = _compilation.GetType("System.Web.UI", "Control");

        Diagnostics.Add(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "WEBFORMS0001",
                    "Could not find type",
                    "Could not find type '{0}' in namespace '{1}'",
                    "WebForms",
                    DiagnosticSeverity.Error,
                    true),
                name.Range,
                name.Value,
                elementNs));

        return type;
    }
}
