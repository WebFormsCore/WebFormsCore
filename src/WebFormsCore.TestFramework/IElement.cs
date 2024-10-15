using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IElement
{
    string Text { get; }

    ValueTask ClickAsync();

    ValueTask TypeAsync(string text);

    ValueTask<bool> IsVisibleAsync();

    ValueTask<string> GetAttributeAsync(string name);
}