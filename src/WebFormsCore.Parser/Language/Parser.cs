﻿using Microsoft.CodeAnalysis;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;
using WebFormsCore.Nodes;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.Language;

public class Parser
{
    private static List<string> IgnoredDirectiveNames = new()
    {
        "Inherits",
        "Language",
        "CodeBehind",
        "CodeFile",
        "Description",
        "LinePragmas",
        "MasterPageFile",
        "Src",
        "Strict"
    };

    private readonly Compilation _compilation;
    private readonly string? _rootNamespace;
    private readonly ParserContainer _rootContainer = new();
    private ParserContainer _container;
    private string? _itemType;
    private readonly Dictionary<string, List<string>> _namespaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ControlKey, (string Type, string Path)> _controlTypes = new(ControlKeyCompare.OrdinalIgnoreCase);
    private INamedTypeSymbol? _type;
    private readonly bool _addFields;
    private readonly string? _rootDirectory;

    public Parser(Compilation compilation, string? rootNamespace, bool addFields, string? rootDirectory = null)
    {
        _compilation = compilation;
        _rootNamespace = rootNamespace;
        _container = _rootContainer;
        _addFields = addFields;
        _rootDirectory = rootDirectory?.Replace('\\', '/');
    }

    public static ReadOnlySpan<char> IncludeSpan => "include".AsSpan();

    public static ReadOnlySpan<char> FileSpan => "file".AsSpan();

    public static ReadOnlySpan<char> VirtualSpan => "virtual".AsSpan();

    public RootNode Root => _container.Root;

    public List<ReportedDiagnostic> Diagnostics { get; } = new();

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
                ConsumeExpression(token);
                break;
            case TokenType.EncodeExpression:
                ConsumeExpression(token, encode: true);
                break;
            case TokenType.EvalExpression:
                ConsumeExpression(token, eval: true);
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
            case TokenType.Comment:
                ConsumeComment(ref lexer, token);
                break;
        }
    }

    private void ConsumeComment(ref Lexer lexer, Token token)
    {
        var span = token.Text.Value.AsSpan().TrimStart();

        if (span.Length == 0 || span[0] != '#')
        {
            return;
        }

        // Check for include
        span = span.Slice(1).TrimStart();

        if (!span.StartsWith(IncludeSpan, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check for file
        var index = span.IndexOf(FileSpan, StringComparison.OrdinalIgnoreCase);

        if (index == -1)
        {
            index = span.IndexOf(VirtualSpan, StringComparison.OrdinalIgnoreCase);
        }

        if (index == -1)
        {
            return;
        }

        span = span.Slice(index + FileSpan.Length);

        // Find attribute value
        index = span.IndexOf('=');

        if (index == -1)
        {
            return;
        }

        span = span.Slice(index + 1).TrimStart();

        if (span.Length == 0 || span[0] is not ('"' or '\''))
        {
            return;
        }

        var quote = span[0];
        span = span.Slice(1);

        var end = span.IndexOf(quote);

        if (end == -1)
        {
            return;
        }

        var path = span.Slice(0, end).ToString();
        var directoryName = Path.GetDirectoryName(lexer.File);

        if (directoryName is null)
        {
            return;
        }

        var fullPath = Path.Combine(directoryName, path);

        if (!File.Exists(fullPath))
        {
            return;
        }

        var text = File.ReadAllText(fullPath);

        var newLexer = new Lexer(fullPath, text.AsSpan());

        fullPath = Path.GetFullPath(fullPath).Replace('\\', '/');

        var includePathRelative = _rootDirectory != null && fullPath.StartsWith(_rootDirectory)
            ? fullPath.Substring(_rootDirectory.Length).TrimStart('/')
            : path;

        if (Root.IncludeFiles.All(i => i.Path != includePathRelative))
        {
            Root.IncludeFiles.Add(new IncludeFile(includePathRelative, RootNode.GenerateHash(text)));
        }

        Parse(ref newLexer);
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

    private void ConsumeExpression(Token token, bool eval = false, bool encode = false)
    {
        var element = new ExpressionNode
        {
            Range = token.Range,
            Text = token.Text,
            IsEval = eval,
            IsEncode = encode,
            ItemType = eval ? _itemType : null,
            VariableName = $"expr{_container.ControlId++}"
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
                    element.Attributes.Add(next.Text, new AttributeValue(false, value));
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

        if (element.DirectiveType is DirectiveType.Import && element.Attributes.TryGetValue("Namespace", out var nsImport))
        {
            Root.Namespaces.Add(nsImport.Value);
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
                    Root.AddFields = _type.ContainingAssembly.Equals(_compilation.Assembly, SymbolEqualityComparer.Default);

                    foreach (var kv in element.Attributes)
                    {
                        var member = _type.GetMemberDeep(kv.Key);

                        if (member is null or { CanWrite: false })
                        {
                            if (!IgnoredDirectiveNames.Contains(kv.Key.Value, StringComparer.OrdinalIgnoreCase))
                            {
                                Diagnostics.Add(
                                    ReportedDiagnostic.Create(
                                        Descriptors.PropertyNotFound,
                                        kv.Key.Range,
                                        kv.Key,
                                        _type.ToDisplayString()));
                            }

                            continue;
                        }

                        element.Properties.Add(new PropertyNode(member, kv.Value, null)
                        {
                            Range = kv.Value.Range
                        });
                    }
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
            else
            {
                Root.Inherits = _compilation.GetType("WebFormsCore.UI.Page");
                Root.Namespace = "WebFormsCore.UI";
            }
        }

        if (element.DirectiveType is DirectiveType.Register &&
            element.Attributes.TryGetValue("tagprefix", out var tagPrefix))
        {
            if (element.Attributes.TryGetValue("namespace", out var ns))
            {
                AddNamespace(tagPrefix, ns);
            }
            else if (element.Attributes.TryGetValue("tagname", out var tagName) &&
                     element.Attributes.TryGetValue("src", out var src))
            {
                RegisterControl(lexer, tagPrefix, tagName, src);
            }
        }
    }

    private void RegisterControl(Lexer lexer, AttributeValue tagPrefix, AttributeValue tagName, AttributeValue src)
    {
        var key = new ControlKey(tagPrefix.Value, tagName.Value);

        if (_controlTypes.ContainsKey(key))
        {
            Diagnostics.Add(
                ReportedDiagnostic.Create(
                    Descriptors.DuplicateControlRegister,
                    src.Range,
                    tagPrefix.Value,
                    tagName.Value));

            return;
        }

        if (_rootDirectory is null)
        {
            return;
        }

        string path;
        string fullPath;

        if (src.Value.StartsWith("~/"))
        {
            path = src.Value.Substring(2);
            fullPath = Path.Combine(_rootDirectory, path);
        }
        else if (src.Value.StartsWith("/"))
        {
            path = src.Value.Substring(1);
            fullPath = Path.Combine(_rootDirectory, path);
        }
        else
        {
            var basePath = Path.GetDirectoryName(lexer.File)!;

            fullPath = Path.Combine(basePath, src.Value).Replace('\\', '/');

            if (_rootDirectory != null && fullPath.StartsWith(_rootDirectory))
            {
                path = fullPath.Substring(_rootDirectory.Length).TrimStart('/');
            }
            else
            {
                path = fullPath;
            }
        }

        if (!File.Exists(fullPath))
        {
            if (TryResolveAssemblyControl(path, key))
            {
                return;
            }

            Diagnostics.Add(ReportedDiagnostic.Create(Descriptors.ControlNotFound, src.Range, lexer.File));
            _controlTypes.Add(key, ("WebFormsCore.UI.Control", path));
            return;
        }

        var text = File.ReadAllText(fullPath); // TODO: Don't read the whole file
        var typeName = RootNode.DetectInherits(text);

        if (typeName is null)
        {
            Diagnostics.Add(ReportedDiagnostic.Create(Descriptors.InheritNotFound, src.Range, lexer.File));

            return;
        }

        _controlTypes.Add(key, (typeName, path));
    }

    private bool TryResolveAssemblyControl(string path, ControlKey key)
    {
        foreach (var referencedAssembly in _compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (var attribute in referencedAssembly.GetAttributes())
            {
                if (attribute.AttributeClass is not { Name: "AssemblyViewAttribute" })
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length >= 2 &&
                    attribute.ConstructorArguments[0].Value?.ToString() == path &&
                    attribute.ConstructorArguments[1].Value is INamedTypeSymbol type)
                {
                    var displayName = type.ToDisplayString(NullableFlowState.None);

                    if (type.BaseType is not null)
                    {
                        foreach (var typeAttribute in type.GetAttributes())
                        {
                            if (typeAttribute.AttributeClass is not { Name: "CompiledViewAttribute" })
                            {
                                continue;
                            }

                            displayName = type.BaseType?.ToDisplayString(NullableFlowState.None);
                        }
                    }

                    if (displayName is not null)
                    {
                        _controlTypes.Add(key, (displayName, path));
                        return true;
                    }
                }
            }
        }

        return false;
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
            _container.Current is ITypedNode { ParseChildren: true } parentControl &&
            parentControl.Type.GetMemberDeep(name.Text) is {} elementMember)
        {
            if (elementMember.Type.IsTemplate())
            {
                var templateNode = new TemplateNode
                {
                    Property = name,
                    ClassName = $"Template_{_type?.Name}_{_container.Current.VariableName}_{name}",
                    ControlsType = attributes.TryGetValue("ControlsType", out var controlsType)
                        ? controlsType.Value
                        : null,
                    ItemType = parentControl is ControlNode collectionNode
                        ? collectionNode.ItemType
                        : null
                };

                parentControl.Templates.Add(templateNode);
                Root.Templates.Add(templateNode);

                node = templateNode;
            }
            else
            {
                var collectionNode = new CollectionNode
                {
                    Property = name.Text,
                    PropertyType = (INamedTypeSymbol)elementMember.Type
                };

                AddAttributes(attributes, collectionNode);

                node = collectionNode;
            }
        }
        else if (runAt == RunAt.Server && !ns.HasValue && name.Text.Value.Equals("script", StringComparison.OrdinalIgnoreCase))
        {
            if (selfClosing)
            {
                return;
            }

            if (lexer.Peek() is { Type: TokenType.Text, Text: var text })
            {
                lexer.Next();
                Root.ScriptBlocks.Add(text);
            }

            if (lexer.Peek() is { Type: TokenType.TagClose })
            {
                lexer.Next();
            }

            return;
        }
        else if (runAt == RunAt.Server || (ns.HasValue && _container.Current is CollectionNode))
        {
            INamedTypeSymbol? controlType = null;
            string? controlPath = null;

            var itemType = attributes.TryGetValue("itemtype", out var itemTypeStr)
                ? _compilation.GetType(itemTypeStr.Value)
                : null;

            if (ns.HasValue && _controlTypes.TryGetValue(new ControlKey(ns.Value.Text, name.Text), out var controlTypeName))
            {
                controlType = _compilation.GetType(controlTypeName.Type);
                controlPath = controlTypeName.Path;
            }
            else
            {
                if (itemType != null)
                {
                    var type = GetControlType(ns?.Text, name.Text + "`1", true);

                    if (type != null)
                    {
                        controlType = type.Construct(itemType);
                    }
                }

                controlType ??= GetControlType(ns?.Text, name.Text);
            }

            controlType ??= _compilation.GetType("WebFormsCore.UI.HtmlGenericControl");

            if (controlType == null)
            {
                return;
            }

            var controlNode = new ControlNode(controlType, controlPath)
            {
                ItemType = itemType
            };

            if (attributes.TryGetValue("id", out var id))
            {
                if (_container.Template == null)
                {
                    var member = _type?.GetMemberDeep(id.Value);

                    if (member is null or { CanWrite: false })
                    {
                        var addToDesigner = !(_addFields && Root.AddFields) || member is { CanWrite: false };
                        Root.Ids.Add(new ControlId(addToDesigner, id, controlType, member));
                        controlNode.FieldName = id;
                    }
                    else
                    {
                        controlNode.FieldName = member?.Name;
                    }
                }
                else
                {
                    _container.Template.Ids.Add(new ControlId(false, id, controlType, null));
                }
            }

            if (controlType.Name == "HtmlGenericControl")
            {
                var member = controlType.GetMemberDeep("TagName");

                if (member != null)
                {
                    controlNode.Properties.Add(new PropertyNode(member, new AttributeValue(false, name.Text), null));
                }
            }

            AddAttributes(attributes, controlNode);

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

    private void AddAttributes(Dictionary<TokenString, AttributeValue> attributes, ITypedNode node)
    {
        var controlType = node.Type;

        foreach (var attribute in attributes)
        {
            var key = attribute.Key.Value;

            if (key.Equals("runat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = attribute.Value;

            if (key.Contains('-'))
            {
                SetAttributeDeep(attribute.Key.Range, node, attribute.Key, value);
                continue;
            }

            if (key.StartsWith("On", StringComparison.OrdinalIgnoreCase))
            {
                var eventSymbol = controlType?.GetDeep<IEventSymbol>(key.Substring(2));
                var method = _type?.GetDeep<IMethodSymbol>(value);

                if (eventSymbol != null && method != null)
                {
                    node.Events.Add(new EventNode(eventSymbol, method)
                    {
                        Range = attribute.Value.Range
                    });
                    continue;
                }
            }

            if (key.Equals("ID", StringComparison.OrdinalIgnoreCase) && node is ControlNode controlNode)
            {
                if (controlType?.GetMemberDeep(key) is { CanWrite: true })
                {
                    controlNode.Id = value;
                }

                continue;
            }

            SetAttribute(node, attribute.Key, attribute.Value);
        }
    }

    private void SetAttributeDeep(TokenRange range, ITypedNode parentNode, TokenString key, AttributeValue value)
    {
        var index = key.Value.IndexOf('-');
        var span = key.Value.AsSpan();
        var keyRange = key.Range;

        var currentNode = parentNode;

        while (index != -1)
        {
            var current = span.Slice(0, index).ToString();
            var property = currentNode.Type.GetMemberDeep(current);

            if (property is null)
            {
                break;
            }

            var next = currentNode.Children
                .OfType<CollectionNode>()
                .FirstOrDefault(i => i.Property == property.Name);

            if (next == null)
            {
                next = new CollectionNode
                {
                    Parent = currentNode as ElementNode,
                    Range = range,
                    Property = property.Name,
                    PropertyType = (INamedTypeSymbol)property.Type,
                    VariableName = $"ctrl{_container.ControlId++}"
                };

                parentNode.Children.Add(next);
            }

            currentNode = next;
            span = span.Slice(index + 1);
            keyRange = keyRange.Slice(index + 1);
            index = span.IndexOf('-');
        }

        var last = span.ToString();

        SetAttribute(currentNode, last, value);
    }

    private void SetAttribute(
        ITypedNode controlNode,
        TokenString key,
        AttributeValue value)
    {
        var controlType = controlNode.Type;

        if (controlType.GetMemberDeep(key) is { CanWrite: true } member)
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

            controlNode.Properties.Add(new PropertyNode(member, value, converter)
            {
                Range = value.Range
            });
            return;
        }

        var implementsAttributeAccessor = controlType.AllInterfaces.Any(x => x.Name == "IAttributeAccessor" && x.ContainingNamespace.ToString() == "WebFormsCore.UI");

        if (implementsAttributeAccessor)
        {
            controlNode.Attributes.Add(key, value);
            return;
        }

        Diagnostics.Add(ReportedDiagnostic.Create(
            Descriptors.PropertyNotFound,
            key.Range,
            key,
            controlType.ToDisplayString()));
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

    private (bool Closed, Dictionary<TokenString, AttributeValue> Attributes) ConsumeAttributes(ref Lexer lexer)
    {
        var attributes = new Dictionary<TokenString, AttributeValue>(AttributeCompare.IgnoreCase);

        while (lexer.Next() is { } keyNode)
        {
            if (keyNode.Type == TokenType.Attribute)
            {
                TokenString value = default;
                var isCode = false;

                if (lexer.Peek() is { Type: TokenType.AttributeValue or TokenType.EvalExpression } valueNode)
                {
                    isCode = valueNode.Type == TokenType.EvalExpression;
                    lexer.Next();
                    value = valueNode.Text;
                }

                var key = keyNode.Text;

                if (key.Value.Equals("itemtype", StringComparison.OrdinalIgnoreCase))
                {
                    _itemType = value.Value;
                }

                attributes.Add(key, new AttributeValue(isCode, value));
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
            var popNamespace = pop.Namespace.HasValue ? pop.Namespace.Value.Value + ":" : null;
            var nameNamespace = endNamespace.HasValue ? endNamespace.Value.Value + ":" : null;

            Diagnostics.Add(
                ReportedDiagnostic.Create(
                    Descriptors.UnexpectedClosingTag,
                    new TokenRange(lexer.File, startPosition, endPosition),
                    $"{popNamespace}{pop.Name}",
                    $"{nameNamespace}{name.Text}"));

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
            // Note: make sure this list is up-to-date with WebObjectActivator.CreateElement

            return name.Value switch
            {
                "form" or "FORM" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlForm"),
                "body" or "BODY" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlBody"),
                "title" or "TITLE" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlTitle"),
                "head" or "HEAD" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlHead"),
                "link" or "LINK" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlLink"),
                "script" or "SCRIPT" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlScript"),
                "style" or "STYLE" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlStyle"),
                "img" or "IMG" => _compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlImage"),
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

        type = _compilation.GetType("WebFormsCore.UI", "Control");

        Diagnostics.Add(
            ReportedDiagnostic.Create(
                Descriptors.TypeNotFoundInNamespace,
                name.Range,
                name.Value,
                elementNs));

        return type;
    }
}
