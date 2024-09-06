using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IElement
{
    string Text { get; }

    ValueTask ClickAsync();

    ValueTask TypeAsync(string text);

    ValueTask<string> GetAttributeAsync(string dataFoo);
}