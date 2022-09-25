﻿using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class TemplateNode : ElementNode
{
    public string ClassName { get; set; }

    public Token Property { get; set; }

    public List<ContainerNode> RenderMethods { get; set; } = new();

    public override string? VariableName
    {
        get => null;
        set
        {
            // ignore.
        }
    }
}