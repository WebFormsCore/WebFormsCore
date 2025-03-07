using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IElement
{
    string Text { get; }

    string Value { get; }

    ValueTask ClearAsync();

    ValueTask ClickAsync();

    ValueTask TypeAsync(string text);

    ValueTask<bool> IsVisibleAsync();

    ValueTask<string> GetAttributeAsync(string name);
}