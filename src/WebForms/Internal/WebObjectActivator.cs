using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

internal sealed class WebObjectActivator : IWebObjectActivator, IDisposable
{
    private readonly ObjectPool<LiteralControl> _literalPool;
    private readonly ObjectPool<HtmlGenericControl> _htmlPool;
    private readonly List<LiteralControl> _literals = new();
    private readonly List<HtmlGenericControl> _htmlControls = new();
    private readonly IServiceProvider _serviceProvider;

    public WebObjectActivator(ObjectPool<LiteralControl> literalPool, IServiceProvider serviceProvider, ObjectPool<HtmlGenericControl> htmlPool)
    {
        _literalPool = literalPool;
        _serviceProvider = serviceProvider;
        _htmlPool = htmlPool;
    }

    public T CreateControl<T>()
    {
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
    }

    public object CreateControl(Type type)
    {
        return ActivatorUtilities.CreateInstance(_serviceProvider, type);
    }

    public LiteralControl CreateLiteral(string text)
    {
        var literal = _literalPool.Get();
        literal.Text = text;
        _literals.Add(literal);
        return literal;
    }

    public LiteralControl CreateLiteral(object? value)
    {
        return CreateLiteral(value?.ToString() ?? "");
    }

    public HtmlGenericControl CreateHtml(string tagName)
    {
        var control = _htmlPool.Get();
        control.TagName = tagName;
        _htmlControls.Add(control);
        return control;
    }

    public void Dispose()
    {
        foreach (var literal in _literals)
        {
            _literalPool.Return(literal);
        }

        foreach (var htmlControl in _htmlControls)
        {
            _htmlPool.Return(htmlControl);
        }
    }
}