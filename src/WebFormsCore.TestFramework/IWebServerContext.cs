using WebFormsCore.UI;

namespace WebFormsCore;

public interface IWebServerContext<out T> : ITestContext<T>
    where T : Control
{
    string Url { get; }
}