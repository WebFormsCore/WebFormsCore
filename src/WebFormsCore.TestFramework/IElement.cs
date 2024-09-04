using System.Threading.Tasks;

namespace WebFormsCore;

public interface IElement
{
    string Text { get; }

    ValueTask ClickAsync();

    ValueTask TypeAsync(string text);
}