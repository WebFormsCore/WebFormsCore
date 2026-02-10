using WebFormsCore.UI;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class AttributeCollectionTest
{
    [Fact]
    public void AddAttribute_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("name", "value");

        Assert.Equal("value", attributes["name"]);
        Assert.True(attributes.ContainsKey("name"));
    }

    [Fact]
    public void AddStyle_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("style", "color: red;");

        Assert.Equal("red", attributes.CssStyle[HtmlTextWriterStyle.Color]);
        Assert.Equal("color: red;", attributes["style"]);
    }

    [Fact]
    public void AddStyleAttribute_Works()
    {
        var attributes = new AttributeCollection();
        attributes.CssStyle.Add(HtmlTextWriterStyle.BackgroundColor, "blue");

        Assert.Equal("background-color:blue;", attributes["style"]);
        Assert.Equal("blue", attributes.CssStyle[HtmlTextWriterStyle.BackgroundColor]);
    }

    [Fact]
    public void RemoveAttribute_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("name", "value");
        attributes.Remove("name");

        Assert.True(attributes["name"].IsEmpty);
        Assert.False(attributes.ContainsKey("name"));
    }

    [Fact]
    public void RemoveStyle_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("style", "color: red;");
        attributes.Remove("style");

        Assert.Equal(0, attributes.CssStyle.Count);
        Assert.True(attributes["style"].IsEmpty);
    }

    [Fact]
    public async Task Render_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("name", "value");
        attributes.Add("style", "color: red;");

        var writer = new StringHtmlTextWriter();
        await attributes.RenderAsync(writer);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("name=\"value\"", output);
        Assert.Contains("style=\"color: red;\"", output);
    }

    [Fact]
    public void AddAttributesToWriter_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("name", "value");
        attributes.CssStyle.Add(HtmlTextWriterStyle.Color, "red");

        var writer = new StringHtmlTextWriter();
        attributes.AddAttributes(writer);

        // AddAttributes adds them to the writer's internal attribute list, it doesn't render them yet.
        // We need to render a tag to see them.
    }

    [Fact]
    public async Task RenderWithTag_Works()
    {
        var attributes = new AttributeCollection();
        attributes.Add("name", "value");
        attributes.CssStyle.Add(HtmlTextWriterStyle.Color, "red");

        var writer = new StringHtmlTextWriter();
        attributes.AddAttributes(writer);
        await writer.RenderBeginTagAsync("div");
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("name=\"value\"", output);
        Assert.Contains("style=\"color: red;\"", output);
    }

    [Fact]
    public void AttributeCollection_CaseInsensitive()
    {
        var attributes = new AttributeCollection();
        attributes["CLASS"] = "foo";
        Assert.Equal("foo", attributes["class"]);
    }

    [Fact]
    public void ViewState_TracksChangesAfterTrackingStarted()
    {
        var attributes = new AttributeCollection();
        var vso = (IViewStateObject)attributes;

        attributes["class"] = "initial";
        
        vso.TrackViewState(default); // Started tracking
        
        Assert.False(vso.WriteToViewState); // No changes yet
        
        attributes["class"] = "changed";
        Assert.True(vso.WriteToViewState);
    }

    [Fact]
    public void ViewState_TracksNewAttributes()
    {
        var attributes = new AttributeCollection();
        var vso = (IViewStateObject)attributes;

        vso.TrackViewState(default);
        
        attributes["data-new"] = "value";
        Assert.True(vso.WriteToViewState);
    }

    [Fact]
    public void Equals_Works()
    {
        var attr1 = new AttributeCollection();
        attr1["a"] = "b";
        attr1.CssStyle["color"] = "red";

        var attr2 = new AttributeCollection();
        attr2["a"] = "b";
        attr2.CssStyle["color"] = "red";

        Assert.Equal(attr1, attr2);
    }
}
