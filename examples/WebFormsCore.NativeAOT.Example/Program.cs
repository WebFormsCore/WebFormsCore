using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.NativeAOT.Example;
using WebFormsCore.NativeAOT.Example.Context;

public static class Runtime
{
    private static int _id = 0;
    private static readonly ConcurrentDictionary<int, ConsoleContext> Results = new();
    private static ServiceProvider? _provider;

    public static IServiceProvider Provider
    {
        get
        {
            if (_provider is not null)
            {
                return _provider;
            }

            var services = new ServiceCollection();

            services.AddOptions();
            services.AddWebFormsInternals();

            _provider = services.BuildServiceProvider();
            return _provider;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "CreateContext")]
    public static int CreateContext()
    {
        var id = Interlocked.Increment(ref _id);
        var scope = Provider.CreateAsyncScope();
        var consoleContext = new ConsoleContext(scope);

        Results[id] = consoleContext;

        return id;
    }

    [UnmanagedCallersOnly(EntryPoint = "Run")]
    public static void Run(int id)
    {
        if (!Results.TryGetValue(id, out var consoleContext))
        {
            return;
        }

        Task.Run(async () =>
        {
            var provider = consoleContext.Scope.ServiceProvider;
            var pageManager = provider.GetRequiredService<IPageManager>();

            await pageManager.RenderPageAsync(
                consoleContext,
                provider,
                "Test.aspx",
                consoleContext.Response.Body,
                CancellationToken.None);

            var array = consoleContext.Response.Body.GetBuffer();
            var length = (int)consoleContext.Response.Body.Length;

            SendResponse(id, array, length);

            Results.TryRemove(id, out _);

            await consoleContext.Scope.DisposeAsync();
        });
    }

    private static unsafe void SendResponse(int id, byte[] data, int length)
    {
        fixed (byte* ptr = data)
        {
            SendResponse(id, ptr, length);
        }
    }

    [DllImport("SendResponse")]
    public static extern unsafe void SendResponse(int id, byte* data, int length);
}
