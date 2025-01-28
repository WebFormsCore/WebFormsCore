using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebFormsCore.UI;

namespace WebFormsCore;

public abstract class WebServerContext<T>(IWebHost host) : IWebServerContext<T>
    where T : Control, new()
{
    private T? _control;
    private string? _url;
    private TaskCompletionSource<bool>? _requestLock;
    private Exception? _exception;

    public T Control
    {
        get
        {
            if (_exception != null)
            {
                throw new InvalidOperationException("An exception occurred while initializing the control", _exception);
            }

            return _control ?? throw new InvalidOperationException("Control is not initialized");
        }
    }

    public Task SetControlAsync(T control)
    {
        var requestLock = new TaskCompletionSource<bool>();
        _control = control;

        var oldLock = Interlocked.Exchange(ref _requestLock, requestLock);
        oldLock?.SetResult(true);

        return requestLock.Task;
    }

    public void SetException(Exception exception)
    {
        _exception = exception;
    }

    public string Url => _url ??= host.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()
        ?.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("Server address is not available");

    protected virtual ValueTask DisposeCoreAsync() => default;

    public async ValueTask DisposeAsync()
    {
        _requestLock?.SetResult(true);
        await host.StopAsync();
        await DisposeCoreAsync();

        if (host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            host.Dispose();
        }
    }

    public abstract Task GoToUrlAsync(string url);

    public abstract ValueTask<string> GetHtmlAsync();

    public abstract ValueTask ReloadAsync();

    public abstract IElement? GetElementById(string id);

    public abstract IElement? QuerySelector(string selector);

    public abstract IAsyncEnumerable<IElement> QuerySelectorAll(string selector);

    public abstract ValueTask<string?> ExecuteScriptAsync(string script);
}
