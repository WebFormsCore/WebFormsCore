﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public sealed class DefaultWebObjectActivator : IWebObjectActivator
{
    private readonly IControlManager _controlManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _cache = new();

    public DefaultWebObjectActivator(IServiceProvider serviceProvider, IControlManager controlManager)
    {
        _serviceProvider = serviceProvider;
        _controlManager = controlManager;
    }

    public T ParseAttribute<T>(string attributeValue)
    {
        var parser = _serviceProvider.GetRequiredService<IAttributeParser<T>>();
        return parser.Parse(attributeValue);
    }

    public T ParseAttribute<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConverter>(string attributeValue)
        where TConverter : TypeConverter
    {
        var converter = _serviceProvider.GetService<TConverter>() ??
                        ActivatorUtilities.CreateInstance<TConverter>(_serviceProvider);

        return (T) converter.ConvertFrom(attributeValue)!;
    }

    public T CreateControl<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
        IControlFactory<T> factory;

        if (_cache.TryGetValue(typeof(T), out var cached))
        {
            factory = Unsafe.As<IControlFactory<T>>(cached);
        }
        else
        {
            factory = _serviceProvider.GetRequiredService<IControlFactory<T>>();
            _cache[typeof(T)] = factory;
        }

        return factory.CreateControl(_serviceProvider);
    }

    public object CreateControl([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        var factoryType = typeof(IControlFactory<>).MakeGenericType(type);
        var factory = (IControlFactory) _serviceProvider.GetRequiredService(factoryType);
        return factory.CreateControl(_serviceProvider)!;
    }

    public object CreateControl(string fullPath)
    {
        if (_controlManager.TryGetPath(fullPath, out var path))
        {
            fullPath = path;
        }

        var type = _controlManager.GetType(fullPath);

#pragma warning disable IL2072
        return CreateControl(type);
#pragma warning restore IL2072
    }

    public LiteralControl CreateLiteral(string text)
    {
        var control = CreateControl<LiteralControl>();
        control.Text = text;
        return control;
    }

    public LiteralControl CreateLiteral(object? value)
    {
        return CreateLiteral(value?.ToString() ?? "");
    }

    public HtmlContainerControl CreateElement(string tagName)
    {
        // Note: make sure this list is up-to-date with Parser.GetControlType

        switch (tagName)
        {
            case "form" or "FORM":
                return CreateControl<HtmlForm>();
            case "head" or "HEAD":
                return CreateControl<HtmlHead>();
            case "title" or "TITLE":
                return CreateControl<HtmlTitle>();
            case "body" or "BODY":
                return CreateControl<HtmlBody>();
            case "link" or "LINK":
                return CreateControl<HtmlLink>();
            case "script" or "SCRIPT":
                return CreateControl<HtmlScript>();
            case "style" or "STYLE":
                return CreateControl<HtmlStyle>();
            case "img" or "IMG":
                return CreateControl<HtmlImage>();
            default:
                var control = CreateControl<LiteralHtmlControl>();
                control.TagName = tagName;
                return control;
        }
    }
}
