﻿using Microsoft.CodeAnalysis;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class PropertyNode : Node
{
    public PropertyNode(MemberResult member, AttributeValue value, INamedTypeSymbol? converter)
        : base(NodeType.Property)
    {
        Member = member;
        Value = value;
        Converter = converter;
    }

    public MemberResult Member { get; set; }

    public AttributeValue Value { get; set; }

    public INamedTypeSymbol? Converter { get; set; }

    public string DisplayType => Member.DisplayType;

    public string? DisplayConverter => Converter?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
