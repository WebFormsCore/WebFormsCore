namespace WebFormsCore.Tests;

internal class AngleSharpElement(IAngleSharpTestContext result, AngleSharp.Dom.IElement element) : IElement
{
    public string Text => element.TextContent;

    public ValueTask ClickAsync()
    {
        return result.PostbackAsync(element);
    }

    public ValueTask TypeAsync(string text)
    {
        if (element.TagName == "input")
        {
            element.SetAttribute("value", text);
        }
        else
        {
            element.TextContent = text;
        }

        return result.PostbackAsync(element);
    }
}
