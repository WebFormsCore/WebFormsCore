﻿namespace WebFormsCore.Nodes;

public class RootNode : ContainerNode
{
    public RootNode()
        : base(NodeType.Root)
    {
    }

    public List<DirectiveNode> AllDirectives { get; set; } = new();

    public List<HtmlNode> AllHtmlNodes { get; set; } = new();

    public List<Node> AllNodes { get; set; } = new();
}
