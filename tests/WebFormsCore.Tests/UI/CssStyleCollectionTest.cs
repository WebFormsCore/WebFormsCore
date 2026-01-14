using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;
using Xunit;
using System.Linq;

namespace WebFormsCore.Tests.UnitTests.UI;

public class CssStyleCollectionTest
{
    private class TestWebControl : WebControl
    {
        public TestWebControl() : base(HtmlTextWriterTag.Div) { }
    }

    [Fact]
    public void AddAndRetrieve()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red";
        Assert.Equal("red", styles["color"]);
        Assert.Equal("red", styles[HtmlTextWriterStyle.Color]);
    }

    [Fact]
    public void EnumKey()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles[HtmlTextWriterStyle.BackgroundColor] = "blue";
        Assert.Equal("blue", styles["background-color"]);
        Assert.Equal("blue", styles[HtmlTextWriterStyle.BackgroundColor]);
    }

    [Fact]
    public void ParseValue()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles.Value = "color:red; font-weight:bold;";
        Assert.Equal("red", styles["color"]);
        Assert.Equal("bold", styles["font-weight"]);
    }

    [Fact]
    public void BuildValue()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red";
        styles["margin"] = "10px";
        var val = styles.Value;
        Assert.Contains("color:red", val);
        Assert.Contains("margin:10px", val);
    }

    [Fact]
    public void Clear()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red";
        styles.Clear();
        Assert.Null(styles["color"]);
        Assert.True(string.IsNullOrEmpty(styles.Value));
    }

    [Fact]
    public void Remove()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red";
        styles.Remove("color");
        Assert.Null(styles["color"]);
    }

    [Fact]
    public void MixedKeys_KeysAndCount()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red"; // string key
        styles[HtmlTextWriterStyle.BackgroundColor] = "blue"; // enum key
        
        Assert.Equal(2, styles.Count);
        var keys = styles.Keys.Cast<string>().ToList();
        Assert.Contains("color", keys);
        Assert.Contains("background-color", keys);
    }

    [Fact]
    public void Equals_Scenarios()
    {
        var control1 = new TestWebControl();
        var control2 = new TestWebControl();
        var styles1 = control1.Attributes.CssStyle;
        var styles2 = control2.Attributes.CssStyle;

        Assert.True(styles1.Equals(styles2));
        Assert.True(styles1 == styles2);

        styles1["color"] = "red";
        Assert.False(styles1.Equals(styles2));
        Assert.True(styles1 != styles2);

        styles2["color"] = "red";
        Assert.True(styles1.Equals(styles2));

        styles1[HtmlTextWriterStyle.FontSize] = "12pt";
        Assert.False(styles1.Equals(styles2));

        styles2[HtmlTextWriterStyle.FontSize] = "12pt";
        Assert.True(styles1.Equals(styles2));
        
        Assert.Equal(styles1.GetHashCode(), styles2.GetHashCode());
    }

    [Fact]
    public void Add_EnumKey_RemovesStringKey()
    {
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        styles["color"] = "red";
        Assert.Equal(1, styles.Count);
        
        styles[HtmlTextWriterStyle.Color] = "blue";
        Assert.Equal(1, styles.Count);
        Assert.Equal("blue", styles["color"]);
        Assert.Equal("blue", styles[HtmlTextWriterStyle.Color]);
    }

    [Fact]
    public async Task RenderAsync_ProducesCorrectOutput()
    {
        var writer = new StringHtmlTextWriter();
        var control = new TestWebControl();
        var styles = control.Attributes.CssStyle;
        
        styles["color"] = "red";
        styles[HtmlTextWriterStyle.BackgroundColor] = "green";
        
        await styles.RenderAsync(writer);
        await writer.FlushAsync();
        
        var output = writer.ToString();
        Assert.Contains("color: red;", output);
        Assert.Contains("background-color: green;", output);
        Assert.StartsWith(" style=\"", output);
        Assert.EndsWith("\"", output);
    }
}
