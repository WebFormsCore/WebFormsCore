using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using WebFormsCore.Language;
using WebFormsCore.Models;
using WebFormsCore.Nodes;

namespace WebFormsCore.Designer;

public record DesignerType(string? Namespace,
    string Name,
    List<DesignerField> Fields,
    List<DesignerEvent> Events,
    RootNode Root,
    INamedTypeSymbol? Type,
    string Hash)
{
    private string? _classCode;
    private string? _initializeCode;

    public string ClassCode
    {
        get
        {
            if (_classCode == null) CreateCode();
            return _classCode!;
        }
    }

    public string InitializeCode
    {
        get
        {
            if (_initializeCode == null) CreateCode();
            return _initializeCode!;
        }
    }

    private void CreateCode()
    {
        var sb = new StringBuilder();
        var context = new CompileContext(sb)
        {
            Type = Type,
            GenerateFields = true
        };

        Root.WriteClass(context);
        _classCode = sb.ToString();

        sb.Clear();

        Root.Write(context);
        _initializeCode = sb.ToString();
    }

    public static DesignerType? Parse(Compilation compilation, string path, string? text)
    {
        if (text == null) return null;

        var lexer = new Lexer(path, text.AsSpan());
        var parser = new Parser();

        parser.Parse(ref lexer);

        var page = parser.Root.AllDirectives.FirstOrDefault(i => i.DirectiveType == DirectiveType.Page);

        if (page == null || !page.Attributes.TryGetValue("inherits", out var inherits))
        {
            return null;
        }

        var inheritIndex = inherits.Value.LastIndexOf('.');
        var inheritNamespace = inheritIndex == -1 ? null : inherits.Value.Substring(0, inheritIndex);
        var inheritName = inheritIndex == -1 ? inherits.Value : inherits.Value.Substring(inheritIndex + 1);
        var fields = new List<DesignerField>();
        var events = new List<DesignerEvent>();
        var inheritsType = compilation.GetTypeByMetadataName(inherits.Value);

        var registers = parser.Root.AllDirectives.Where(i => i.DirectiveType == DirectiveType.Register);
        var namespaces = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var register in registers)
        {
            if (!register.Attributes.TryGetValue("tagprefix", out var tagPrefix) ||
                !register.Attributes.TryGetValue("namespace", out var ns))
            {
                continue;
            }

            if (!namespaces.TryGetValue(tagPrefix, out var list))
            {
                list = new List<string>();
                namespaces.Add(tagPrefix, list);
            }

            list.Add(ns);
        }

        foreach (var htmlNode in parser.Root.AllHtmlNodes)
        {
            if (htmlNode.RunAt != RunAt.Server)
            {
                continue;
            }

            var type = TryGetType(compilation, htmlNode, namespaces, parser);

            if (type == null) continue;
            
            htmlNode.ControlType = type;

            if (htmlNode.Attributes.TryGetValue("id", out var id))
            {
                var member = inheritsType?.GetMemberDeep(id.Value);

                fields.Add(new DesignerField(
                    id.Value,
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    member == null || member.CanWrite,
                    member == null
                ));
            }
        }

        string hash;

        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();

            foreach (var c in hashBytes)
            {
                sb.Append(c.ToString("X2"));
            }

            hash = sb.ToString();
        }

        return new DesignerType(

            inheritNamespace,
            inheritName,
            fields,
            events,
            parser.Root,
            inheritsType,
            hash
        );
    }

    private static INamedTypeSymbol? TryGetType(Compilation compilation, HtmlNode htmlNode, Dictionary<string, List<string>> namespaces, Parser parser)
    {
        if (htmlNode.Namespace is not { } elementNs)
        {
            return htmlNode.Name.Value.ToUpperInvariant() switch
            {
                "FORM" => compilation.GetTypeByMetadataName("WebFormsCore.UI.WebControls.HtmlForm"),
                _ => compilation.GetTypeByMetadataName("WebFormsCore.UI.HtmlControls.HtmlGenericControl")
            };
        }

        INamedTypeSymbol? type;
        
        if (namespaces.TryGetValue(elementNs, out var list))
        {
            foreach (var ns in list)
            {
                type = compilation.GetType(ns, htmlNode.Name.Value);

                if (type != null)
                {
                    return type;
                }
            }
        }

        type = compilation.GetType("System.Web.UI", "Control");

        parser.Diagnostics.Add(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "WEBFORMS0001",
                    "Could not find type",
                    "Could not find type '{0}' in namespace '{1}'",
                    "WebForms", 
                    DiagnosticSeverity.Error,
                    true),
                htmlNode.Range,
                htmlNode.Name.Value,
                elementNs));

        return type;
    }
}
