using System.Threading.Tasks;

namespace WebFormsCore;

public interface ITestContext
{
    ValueTask<string> GetHtmlAsync();

    ValueTask ReloadAsync();

    IElement? GetElementById(string id);

    IElement? QuerySelector(string selector);

    IElement[] QuerySelectorAll(string selector);
}