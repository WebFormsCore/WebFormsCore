using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface ITestContext
{
    ValueTask<string> GetHtmlAsync();

    ValueTask ReloadAsync();

    IElement? GetElementById(string id);

    IElement? QuerySelector(string selector);

    IAsyncEnumerable<IElement> QuerySelectorAll(string selector);

    ValueTask<string?> ExecuteScriptAsync(string script);
}