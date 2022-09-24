﻿using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public interface IHtmlNode
{
    string? VariableName { get; }

    INamedTypeSymbol? ControlType { get; }
}

public record SimpleHtmlNode(string VariableName, INamedTypeSymbol? ControlType) : IHtmlNode
{
    public override string ToString()
    {
        return VariableName;
    }
}

public class HtmlNode : ContainerNode, IAttributeNode, IHtmlNode
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

    public string? VariableName { get; set; }
    
    public bool SetTag { get; set; }

    public override void WriteClass(CompileContext context)
    {
        if (TemplateClass != null)
        {
            return;
        }

        if (IsServerScript)
        {
            foreach (var child in Children)
            {
                child.WriteRaw(context);
            }
        }
        else
        {
            VariableName = context.GetNext();

            foreach (var child in Children)
            {
                if (!TryWriteTemplate(child, context))
                {
                    child.WriteClass(context);
                }
            }
        }
    }

    private bool TryWriteTemplate(Node child, CompileContext context)
    {
        if (child is not HtmlNode childNode) return false;

        var property = ControlType?.GetMemberDeep(childNode.Name.Value);

        if (property is not { CanWrite: true }) return false;

        var type = property.Type;

        if (type.Name != "ITemplate")
        {
            return false;
        }

        var builder = context.Builder;

        var id = Attributes.TryGetValue("id", out var value)
            ? value.Value
            : (context.TemplateId++).ToString();

        var className = $"Template{id}_{childNode.Name.Value}";

        builder.Append("private class ");
        builder.Append(className);
        builder.AppendLine(" : global::WebFormsCore.UI.ITemplate");
        builder.AppendLine("{");

        builder.AppendLine("public WebFormsCore.IWebObjectActivator WebActivator;");

        var parent = (context.ParentNode, context.Type, context.GenerateFields);

        context.Type = null;
        context.GenerateFields = false;

        foreach (var node in childNode.Children)
        {
            node.WriteClass(context);
        }

        var parameterName = $"container{id}";

        builder.Append("public void InstantiateIn(global::WebFormsCore.UI.Control ");
        builder.Append(parameterName);
        builder.AppendLine(")");
        builder.AppendLine("{");

        context.ParentNode = new SimpleHtmlNode(parameterName, null);

        foreach (var node in childNode.Children)
        {
            node.Write(context);
        }

        (context.ParentNode, context.Type, context.GenerateFields) = parent;

        builder.AppendLine("}");
        builder.AppendLine("}");

        childNode.TemplateClass = className;

        return true;
    }

    public string? TemplateClass { get; set; }

    public override void Write(CompileContext context)
    {
        if (IsServerScript) return;

        var builder = context.Builder;
        var parentNode = context.ParentNode;

        if (VariableName == null) return;

        builder.Append("var ");
        builder.Append(VariableName);

        builder.Append(" = WebActivator.");

        if (ControlType == null)
        {
            builder.Append("CreateHtml(");
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
                builder.Append("CreateHtml(");
                builder.Append(Name.CodeString);
                builder.AppendLine(");");
            }
            else
            {
                builder.Append("CreateControl<");
                builder.Append(ControlType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                builder.AppendLine(">();");
            }

            foreach (var kv in Attributes)
            {
                var key = kv.Key.Value;

                if (key.StartsWith("On", StringComparison.OrdinalIgnoreCase))
                {
                    var eventSymbol = ControlType.GetDeep<IEventSymbol>(key.Substring(2));
                    var method = context.Type?.GetDeep<IMethodSymbol>(kv.Value.Value);

                    if (eventSymbol != null && method != null)
                    {
                        var type = eventSymbol.Type;
                        var invoke = type.GetDeep<IMethodSymbol>("Invoke");

                        builder.Append(VariableName);
                        builder.Append(".");
                        builder.Append(eventSymbol.Name);
                        builder.Append(" += ");

                        if (invoke != null && !method.ReturnType.Equals(invoke.ReturnType, SymbolEqualityComparer.Default))
                        {
                            builder.Append("(");
                            for (var i = 0; i < invoke.Parameters.Length; i++)
                            {
                                if (i > 0) builder.Append(", ");
                                var parameter = invoke.Parameters[i];
                                builder.Append(parameter.Name);
                            }
                            builder.Append(") => { this.");
                            builder.Append(method.Name);
                            builder.Append("(");
                            for (var i = 0; i < invoke.Parameters.Length; i++)
                            {
                                if (i > 0) builder.Append(", ");
                                var parameter = invoke.Parameters[i];
                                builder.Append(parameter.Name);
                            }
                            builder.Append(");");

                            if (invoke.ReturnType.SpecialType != SpecialType.System_Void)
                            {
                                builder.Append(" return ");

                                if (invoke.Name == "Task")
                                {
                                    builder.Append("System.Threading.Tasks.Task.CompletedTask;");
                                }
                                else
                                {
                                    builder.Append("default(");
                                    builder.Append(invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                                    builder.Append(");");
                                }
                            }

                            builder.Append(" }");
                        }
                        else
                        {
                            builder.Append("this.");
                            builder.Append(method.Name);
                        }

                        builder.AppendLine(";");

                        continue;
                    }
                }

                var field = ControlType.GetMemberDeep(key);

                if (field == null)
                {
                    AddAttribute(builder, kv);
                    continue;
                }

                builder.Append(VariableName);
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

            if (parentField is { CanWrite: true } || parentField == null && context.GenerateFields)
            {
                builder.Append("this.");
                builder.Append(parentField?.Name ?? id.Value);
                builder.Append(" = ");
                builder.Append(VariableName);
                builder.AppendLine(";");
            }
        }

        builder.Append(parentNode);
        builder.Append(".AddParsedSubObject(");
        builder.Append(VariableName);
        builder.AppendLine(");");

        context.ParentNode = this;

        foreach (var child in Children)
        {
            if (child is HtmlNode { TemplateClass: { } templateClass } node)
            {
                builder.Append(VariableName);
                builder.Append(".");
                builder.Append(node.Name);
                builder.Append(" = new ");
                builder.Append(templateClass);
                builder.AppendLine("() { WebActivator = WebActivator };");
                continue;
            }

            child.Write(context);
        }

        context.ParentNode = parentNode;
    }

    private void AddAttribute(StringBuilder builder,  KeyValuePair<TokenString, TokenString> keyValue)
    {
        builder.Append("((WebFormsCore.UI.IAttributeAccessor)");
        builder.Append(VariableName);
        builder.Append(").SetAttribute(");
        builder.Append(keyValue.Key.CodeString);
        builder.Append(", ");
        builder.Append(keyValue.Value.CodeString);
        builder.AppendLine(");");
    }

    public override string ToString()
    {
        return VariableName ?? Name.Value;
    }
}
