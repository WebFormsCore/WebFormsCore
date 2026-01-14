using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class ViewStateUnitTests
{
    [Fact]
    public void AttributeCollection_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new ViewStateProvider(serviceProvider);

        var attributes = new AttributeCollection();
        ((IViewStateObject)attributes).TrackViewState(provider);

        attributes["key1"] = "value1";
        attributes["key2"] = "value2";
        attributes["key1"] = "newvalue1";
        attributes["key3"] = "value3";

        // Write
        var writer = new ViewStateWriter(serviceProvider);
        byte[] data;
        try
        {
            ((IViewStateObject)attributes).WriteViewState(ref writer);
            data = writer.Span.ToArray();
        }
        finally
        {
            writer.Dispose();
        }

        // Read
        var newAttributes = new AttributeCollection();
        using (var owner = new ViewStateReaderOwner(data, serviceProvider))
        {
            var reader = owner.CreateReader();
            ((IViewStateObject)newAttributes).ReadViewState(ref reader);
        }

        Assert.Equal("newvalue1", newAttributes["key1"]);
        Assert.Equal("value2", newAttributes["key2"]);
        Assert.Equal("value3", newAttributes["key3"]);
    }

    [Fact]
    public void CssStyleCollection_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new ViewStateProvider(serviceProvider);

        var attributes = new AttributeCollection();
        ((IViewStateObject)attributes).TrackViewState(provider);

        var style = attributes.CssStyle;
        style[HtmlTextWriterStyle.Color] = "red";
        style["custom"] = "value";
        
        style[HtmlTextWriterStyle.Color] = "blue";
        style["new-custom"] = "new-value";

        // Write
        var writer = new ViewStateWriter(serviceProvider);
        byte[] data;
        try
        {
            ((IViewStateObject)attributes).WriteViewState(ref writer);
            data = writer.Span.ToArray();
        }
        finally
        {
            writer.Dispose();
        }

        // Read
        var newAttributes = new AttributeCollection();
        using (var owner = new ViewStateReaderOwner(data, serviceProvider))
        {
            var reader = owner.CreateReader();
            ((IViewStateObject)newAttributes).ReadViewState(ref reader);
        }

        Assert.Equal("blue", newAttributes.CssStyle[HtmlTextWriterStyle.Color]);
        Assert.Equal("value", newAttributes.CssStyle["custom"]);
        Assert.Equal("new-value", newAttributes.CssStyle["new-custom"]);
    }

    [Fact]
    public void AttributeCollection_PredefinedAndCustom_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new ViewStateProvider(serviceProvider);

        var attributes = new AttributeCollection();
        ((IViewStateObject)attributes).TrackViewState(provider);

        attributes["class"] = "my-class"; // predefined
        attributes["custom-attr"] = "custom-value"; // custom

        // Write
        var writer = new ViewStateWriter(serviceProvider);
        byte[] data;
        try
        {
            ((IViewStateObject)attributes).WriteViewState(ref writer);
            data = writer.Span.ToArray();
        }
        finally
        {
            writer.Dispose();
        }

        // Read
        var newAttributes = new AttributeCollection();
        using (var owner = new ViewStateReaderOwner(data, serviceProvider))
        {
            var reader = owner.CreateReader();
            ((IViewStateObject)newAttributes).ReadViewState(ref reader);
        }

        Assert.Equal("my-class", newAttributes["class"]);
        Assert.Equal("custom-value", newAttributes["custom-attr"]);
    }
}
