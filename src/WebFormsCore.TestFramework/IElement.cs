using System.Threading.Tasks;

namespace WebFormsCore;

public interface IElement
{
    string Text { get; }

    string Value { get; }

    ValueTask ClearAsync();

    ValueTask ClickAsync();

    ValueTask SelectAsync(string value);

    ValueTask PostBackAsync(string? argument = null, PostBackOptions? options = null);

    ValueTask TypeAsync(string text);

    ValueTask<bool> IsVisibleAsync();

    ValueTask<string> GetAttributeAsync(string name);
}