using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class LiteralUnitTests
{
    [Fact]
    public async Task Literal_PassThroughMode_RendersRaw()
    {
        var literal = new Literal
        {
            Text = "<b>test</b>",
            Mode = LiteralMode.PassThrough
        };

        var sw = new StringHtmlTextWriter();
        await literal.RenderAsync(sw, default);
        await sw.FlushAsync();

        Assert.Equal("<b>test</b>", sw.ToString());
    }

    [Fact]
    public async Task Literal_EncodeMode_EncodesText()
    {
        var literal = new Literal
        {
            Text = "<b>test</b>",
            Mode = LiteralMode.Encode
        };

        var sw = new StringHtmlTextWriter();
        await literal.RenderAsync(sw, default);
        await sw.FlushAsync();

        Assert.Equal("&lt;b&gt;test&lt;/b&gt;", sw.ToString());
    }

    [Fact]
    public async Task LiteralControl_RendersRawText()
    {
        var literal = new LiteralControl { Text = "<b>test</b>" };

        var sw = new StringHtmlTextWriter();
        await literal.RenderAsync(sw, default);
        await sw.FlushAsync();

        Assert.Equal("<b>test</b>", sw.ToString());
    }
}
