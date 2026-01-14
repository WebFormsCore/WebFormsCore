using System.Text;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI.WebControls;

public class WebControlUnitTests
{
    private class TestWebControl : WebControl
    {
        public TestWebControl(HtmlTextWriterTag tag) : base(tag) { }

        public override string? DisabledCssClass => null;

        public ValueTask PublicAddAttributesToRender(HtmlTextWriter writer)
            => AddAttributesToRender(writer, default);
    }

    [Fact]
    public async Task Render_IncludesAttributes()
    {
        var control = new TestWebControl(HtmlTextWriterTag.Div)
        {
            ID = "test",
            CssClass = "my-class",
            ToolTip = "hint",
            TabIndex = 1
        };

        var sw = new StringHtmlTextWriter();
        
        await control.RenderAsync(sw, default);
        await sw.FlushAsync();

        var output = sw.ToString();
        Assert.Contains("id=\"test\"", output);
        Assert.Contains("class=\"my-class\"", output);
        Assert.Contains("title=\"hint\"", output);
        Assert.Contains("tabindex=\"1\"", output);
    }

    [Fact]
    public async Task Enabled_AffectsRendering()
    {
        var control = new TestWebControl(HtmlTextWriterTag.Input)
        {
            Enabled = false
        };

        var sw = new StringHtmlTextWriter();
        
        await control.RenderAsync(sw, default);
        await sw.FlushAsync();

        var output = sw.ToString();
        Assert.Contains("disabled=\"disabled\"", output);
    }

    [Fact]
    public void Attributes_AreCaseInsensitive()
    {
        var control = new TestWebControl(HtmlTextWriterTag.Div);
        control.Attributes["DATA-TEST"] = "value";
        
        Assert.Equal("value", control.Attributes["data-test"]);
    }
}
