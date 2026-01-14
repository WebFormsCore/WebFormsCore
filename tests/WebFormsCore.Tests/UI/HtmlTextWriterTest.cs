using System.Text;
using WebFormsCore.UI;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WebFormsCore.Tests.UnitTests.UI;

public class HtmlTextWriterTest
{
    [Fact]
    public void RenderTag()
    {
        var writer = new StringHtmlTextWriter();

        writer.RenderBeginTag(HtmlTextWriterTag.Div);
        writer.Write("Hello");
        writer.RenderEndTag();
        writer.Flush();

        Assert.Equal("<div>Hello</div>\n", writer.ToString());
    }

    [Fact]
    public void RenderAttributes()
    {
        var writer = new StringHtmlTextWriter();

        writer.AddAttribute(HtmlTextWriterAttribute.Class, "my-class");
        writer.AddAttribute("data-custom", "value");
        writer.RenderBeginTag(HtmlTextWriterTag.Span);
        writer.RenderEndTag();
        writer.Flush();

        Assert.Equal("<span class=\"my-class\" data-custom=\"value\"></span>\n", writer.ToString());
    }

    [Fact]
    public void RenderStyles()
    {
        var writer = new StringHtmlTextWriter();

        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "red");
        writer.AddStyleAttribute("font-weight", "bold");
        writer.RenderBeginTag(HtmlTextWriterTag.Div);
        writer.RenderEndTag();
        writer.Flush();

        Assert.Equal("<div style=\"Color:red; font-weight:bold; \"></div>\n", writer.ToString());
    }

    [Fact]
    public async Task RenderTagAsync()
    {
        var writer = new StringHtmlTextWriter();

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await writer.WriteAsync("Hello");
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();

        Assert.Equal("<div>Hello</div>\n", writer.ToString());
    }

    [Fact]
    public async Task RenderAttributesAsync()
    {
        var writer = new StringHtmlTextWriter();

        writer.AddAttribute(HtmlTextWriterAttribute.Href, "https://example.com");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.A);
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();

        Assert.Equal("<a href=\"https://example.com\"></a>\n", writer.ToString());
    }

    [Fact]
    public async Task RenderStylesAsync()
    {
        var writer = new StringHtmlTextWriter();

        writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, "10px");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();

        Assert.Equal("<div style=\"Margin:10px; \"></div>\n", writer.ToString());
    }

    [Fact]
    public void WriteEncodedText()
    {
        var writer = new StringHtmlTextWriter();

        writer.WriteEncodedText("<b>Bold</b>");
        writer.Flush();

        Assert.Equal("&lt;b&gt;Bold&lt;/b&gt;", writer.ToString());
    }

    [Fact]
    public async Task WriteSpanFormattableAsync()
    {
        var writer = new StringHtmlTextWriter();
        await writer.WriteObjectAsync(1234, false); // ISpanFormattable
        await writer.FlushAsync();
        Assert.Equal("1234", writer.ToString());
    }

    [Fact]
    public void WriteLargeOutput_TriggersBufferGrow()
    {
        var writer = new StringHtmlTextWriter();
        var largeString = new string('a', 4000);
        writer.Write(largeString);
        writer.Flush();
        Assert.Equal(largeString, writer.ToString());
    }

    [Fact]
    public async Task SelfClosingTagAsync()
    {
        var writer = new StringHtmlTextWriter();

        await writer.RenderSelfClosingTagAsync(HtmlTextWriterTag.Img);
        await writer.FlushAsync();

        Assert.Equal("<img />", writer.ToString());
    }

    [Fact]
    public async Task RenderStylesAsync_NoDuplicates()
    {
        var writer = new StringHtmlTextWriter();
        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "red");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();
        
        var output = writer.ToString();
        Assert.Single(Regex.Matches(output, "style="));
        Assert.Single(Regex.Matches(output, "Color:red"));
    }

    [Fact]
    public void WriteVariousTypes()
    {
        var writer = new StringHtmlTextWriter();
        writer.Write(true.ToString());
        writer.Write(' ');
        writer.Write(123.ToString());
        writer.Write(' ');
        writer.Write(456L.ToString());
        writer.Write(' ');
        writer.Write(7.89f.ToString());
        writer.Write(' ');
        writer.Write(10.11d.ToString());
        writer.Flush();

        Assert.Equal($"{true} 123 456 {7.89f} {10.11d}", writer.ToString());
    }

    [Fact]
    public void WriteBreak()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteBreak();
        writer.Flush();
        Assert.Equal("<br />", writer.ToString());
    }

    [Fact]
    public void GrowBuffer()
    {
        var writer = new StringHtmlTextWriter();
        var largeString = new string('a', 5000);
        writer.Write(largeString);
        writer.Flush();
        Assert.Equal(largeString, writer.ToString());
    }

    [Fact]
    public void AddAttribute_String()
    {
        var writer = new StringHtmlTextWriter();
        writer.AddAttribute("key", "value");
        writer.RenderBeginTag("div");
        writer.RenderEndTag();
        writer.Flush();
        Assert.Equal("<div key=\"value\"></div>\n", writer.ToString());
    }

    [Fact]
    public async Task RenderBeginTag_CustomTags()
    {
        var writer = new StringHtmlTextWriter();
        await writer.RenderBeginTagAsync("my-tag");
        await writer.RenderEndTagAsync();
        await writer.FlushAsync();
        Assert.Equal("<my-tag></my-tag>\n", writer.ToString());
    }

    [Fact]
    public void WriteEncodedUrl()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteEncodedUrl("test 1.aspx?a=1&b= 2");
        writer.Flush();
        // Encodes before ?, but not after
        Assert.Equal("test%201.aspx?a=1&b= 2", writer.ToString());
    }

    [Fact]
    public void WriteEndTag_Manual()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteBeginTag("div");
        writer.Write('>');
        writer.WriteEndTag("div");
        writer.Flush();
        Assert.Equal("<div></div>", writer.ToString());
    }

    [Fact]
    public void WriteEncodedAttribute()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteAttribute("title", "Quotes \" and <brackets>");
        writer.Flush();
        Assert.Equal(" title=\"Quotes &quot; and &lt;brackets&gt;\"", writer.ToString());
    }

    [Fact]
    public void WriteEncodedText_Large()
    {
        var writer = new StringHtmlTextWriter();
        var largeString = new string('<', 2000) + new string('>', 2000);
        writer.WriteEncodedText(largeString);
        writer.Flush();
        var output = writer.ToString();
        Assert.Equal(new string('<', 2000).Replace("<", "&lt;") + new string('>', 2000).Replace(">", "&gt;"), output);
    }

    [Fact]
    public void WriteObject_SpanFormattable()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteObject(123); // Int32 is ISpanFormattable
        writer.Flush();
        Assert.Equal("123", writer.ToString());
    }

    [Fact]
    public void WriteEncodedUrl_EscapesCorrectly()
    {
        var writer = new StringHtmlTextWriter();
        writer.WriteEncodedUrl("http://example.com/a b?c d=e f");
        writer.Flush();
        
        // EscapeDataString escapes : and /
        // http%3A%2F%2Fexample.com%2Fa%20b?c d=e f
        var output = writer.ToString();
        Assert.Contains("%3A%2F%2F", output);
        Assert.EndsWith("?c d=e f", output);
    }

    [Fact]
    public async Task WriteAsync_LargeBuffer_SlowPath()
    {
        var writer = new StringHtmlTextWriter();
        var largeString = new string('a', 6000);
        await writer.WriteAsync(largeString);
        await writer.FlushAsync();
        
        Assert.Equal(6000, writer.ToString().Length);
    }

    [Fact]
    public async Task WriteObjectAsync_SpanFormattable_SlowPath()
    {
        var writer = new StringHtmlTextWriter();

        // Force slow path by using a value that doesn't fit in remaining buffer if we fill it first
        var fill = new string('a', 5120 - 3); 
        writer.Write(fill);
        
        // Now only 3 chars left in buffer
        await writer.WriteObjectAsync(123456, encode: false);
        await writer.FlushAsync();
        
        Assert.Equal(fill + "123456", writer.ToString());
    }

    [Fact]
    public void Write_ByteSpan()
    {
        var writer = new StringHtmlTextWriter();
        var bytes = Encoding.UTF8.GetBytes("hello");
        writer.Write(bytes.AsSpan());
        writer.Flush();
        
        Assert.Equal("hello", writer.ToString());
    }

    [Fact]
    public async Task WriteAsync_ByteMemory()
    {
        var writer = new StringHtmlTextWriter();
        var bytes = Encoding.UTF8.GetBytes("hello");
        await writer.WriteAsync(bytes.AsMemory());
        await writer.FlushAsync();
        
        Assert.Equal("hello", writer.ToString());
    }
}
