using AngleSharp.Html.Dom;

namespace WebFormsCore.TestFramework.AngleSharp;

internal class AngleSharpElement(IAngleSharpTestContext result, global::AngleSharp.Dom.IElement element) : IElement
{
    public string Text => element.TextContent;

    public ValueTask ClickAsync()
    {
        switch (element)
        {
            case IHtmlAnchorElement:
            case IHtmlButtonElement:
            case IHtmlInputElement input when input.Type.Is("submit", "reset", "button"):
                return result.PostbackAsync(element);

            case IHtmlInputElement input when input.Type.Is("submit", "reset", "button"):
                input.IsChecked = !input.IsChecked;
                return result.PostbackAsync(element);

            default:
                throw new NotSupportedException($"Element {element.TagName} does not support click.");
        }
    }

    public ValueTask TypeAsync(string text)
    {
        switch (element)
        {
            case IHtmlInputElement input when input.Type.Is(["text", "password", "search", "tel", "url", "email", "date", "month", "week", "time", "datetime-local", "number", "range", "color"]):
                input.Value = text;
                break;

            case IHtmlTextAreaElement textArea:
                textArea.Value = text;
                break;

            default:
                throw new NotSupportedException($"Element {element.TagName} does not support typing.");
        }

        return result.PostbackAsync(element);
    }

    public ValueTask<string> GetAttributeAsync(string dataFoo)
    {
        return ValueTask.FromResult(element.GetAttribute(dataFoo) ?? "");
    }
}
