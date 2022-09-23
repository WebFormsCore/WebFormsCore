using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

internal sealed class WebObjectActivator : IWebObjectActivator
{
    private readonly IServiceProvider _serviceProvider;

    public WebObjectActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T CreateControl<T>()
    {
        var factory = _serviceProvider.GetRequiredService<IControlFactory<T>>();

        return factory.CreateControl(_serviceProvider);
    }

    public object CreateControl(Type type)
    {
        return ActivatorUtilities.CreateInstance(_serviceProvider, type);
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

    public HtmlGenericControl CreateHtml(string tagName)
    {
        var control = CreateControl<LiteralHtmlControl>();
        control.TagName = tagName;
        return control;
    }
}
