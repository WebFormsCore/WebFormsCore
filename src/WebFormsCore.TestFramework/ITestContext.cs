using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface ITestContext : IAsyncDisposable
{
    /// <summary>
    /// Gets the last exception that occurred during server-side processing.
    /// </summary>
    Exception? LastException { get; }

    ValueTask<string> GetHtmlAsync();

    ValueTask ReloadAsync();

    IElement? GetElementById(string id);

    IElement? QuerySelector(string selector);

    IAsyncEnumerable<IElement> QuerySelectorAll(string selector);

    ValueTask<string?> ExecuteScriptAsync(string script);
}