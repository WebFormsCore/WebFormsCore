using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using WebForms.Collections;
using WebForms.Models;
using static System.Net.Mime.MediaTypeNames;

namespace WebForms.Nodes;

public class HtmlNode : ContainerNode, IAttributeNode
{
    public HtmlNode()
        : base(NodeType.Html)
    {
    }

    public bool IsServerScript => Name.Value.Equals("script", StringComparison.OrdinalIgnoreCase) && RunAt == RunAt.Server;

    public TokenString Name => StartTag.Name;

    public TokenString? Namespace => StartTag.Namespace;

    public HtmlTagNode StartTag { get; set; } = new();

    public HtmlTagNode? EndTag { get; set; }

    public RunAt RunAt { get; set; } = RunAt.Client;

    public Dictionary<TokenString, TokenString> Attributes { get; set; } = new(AttributeCompare.IgnoreCase);
    
    public INamedTypeSymbol? ControlType { get; set; }

    public string? ControlId { get; set; }
    
    public bool SetTag { get; set; }

    public override void WriteClass(CompileContext context)
    {
        if (IsServerScript)
        {
            foreach (var child in Children)
            {
                child.Write(context);
            }
        }
        else
        {
            ControlId = context.GetNext();
            base.WriteClass(context);
        }
    }

    public override void Write(CompileContext context)
    {
        Debug.Assert(ControlId != null);

        var builder = context.Builder;
        var parentNode = context.ParentNode;

        builder.Append("var ");
        builder.Append(ControlId);

        if (ControlType == null)
        {
            builder.Append(" = WebActivator.CreateHtml(");
            builder.Append(Name.CodeString);
            builder.AppendLine(");");

            foreach (var kv in Attributes)
            {
                AddAttribute(builder, kv);
            }
        }
        else
        {
            if (ControlType.Name == "HtmlGenericControl")
            {
                builder.Append(" = WebActivator.CreateHtml(");
                builder.Append(Name.CodeString);
                builder.AppendLine(");");
            }
            else
            {
                builder.Append(" = WebActivator.CreateControl<");
                builder.Append(ControlType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                builder.AppendLine(">();");
            }

            foreach (var kv in Attributes)
            {
                var field = ControlType.GetMemberDeep(kv.Key.Value);

                if (field == null)
                {
                    AddAttribute(builder, kv);
                    continue;
                }

                builder.Append(ControlId);
                builder.Append(".");
                builder.Append(field.Name);
                builder.Append(" = ");

                if (field.Type.TypeKind == TypeKind.Enum)
                {
                    builder.Append(field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    builder.Append('.');
                    builder.Append(kv.Value.Value);
                }
                else if (field.Type.Name == "String")
                {
                    builder.Append(kv.Value.CodeString);
                }
                else if (field.Type.Name == "Boolean")
                {
                    builder.Append(kv.Value.Value.ToLowerInvariant() is "true" or "1" or "yes" ? "true" : "false");
                }
                else
                {
                    builder.Append(kv.Value.Value);
                }

                builder.AppendLine(";");
            }
        }

        if (RunAt == RunAt.Server && Attributes.TryGetValue("id", out var id))
        {
            var parentField = context.Type?.GetMemberDeep(id.Value);

            if (parentField is { CanWrite: true })
            {
                builder.Append("this.");
                builder.Append(parentField.Name);
                builder.Append(" = ");
                builder.Append(ControlId);
                builder.AppendLine(";");
            }
        }

        context.ParentNode = ControlId!;

        foreach (var child in Children)
        {
            child.Write(context);
        }

        context.ParentNode = parentNode;

        builder.Append(parentNode);
        builder.Append(".AddParsedSubObject(");
        builder.Append(ControlId);
        builder.AppendLine(");");
    }

    private void AddAttribute(StringBuilder builder, KeyValuePair<TokenString, TokenString> keyValue)
    {
        builder.Append(ControlId);
        builder.Append(".Attributes[");
        builder.Append(keyValue.Key.CodeString);
        builder.Append("] = ");
        builder.Append(keyValue.Value.CodeString);
        builder.AppendLine(";");
    }
}
