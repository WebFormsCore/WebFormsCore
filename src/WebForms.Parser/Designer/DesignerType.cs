using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using WebFormsCore.Language;
using WebFormsCore.Models;
using WebFormsCore.Nodes;

namespace WebFormsCore.Designer;

public record DesignerType(
    string? Namespace,
    string? VbNamespace,
    string Name,
    List<DesignerField> Fields,
    List<DesignerEvent> Events,
    RootNode Root,
    INamedTypeSymbol? Type,
    string Hash,
    string Path)
{
    public static RootNode? Parse(Compilation compilation, string path, string? text, string? rootNamespace = null)
    {
        if (text == null) return null;

        var lexer = new Lexer(path, text.AsSpan());
        var parser = new Parser(compilation, rootNamespace);

        parser.Parse(ref lexer);

        parser.Root.Path = path;

        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();

            foreach (var c in hashBytes)
            {
                sb.Append(c.ToString("X2"));
            }

            parser.Root.Hash = sb.ToString();
        }

        return parser.Root;
    }
}
