using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.UIUnitTests;

public class ScopedControlContainerTest
{
    [Fact]
    public async Task DisposeAsync_DisposesAllControls()
    {
        var container = new ScopedControlContainer();
        var disposable = new SyncDisposableControl();
        var asyncDisposable = new AsyncDisposableControl();

        container.Register(disposable);
        container.Register(asyncDisposable);

        await container.DisposeAsync();

        Assert.True(disposable.IsDisposed);
        Assert.True(asyncDisposable.IsDisposed);
    }

    [Fact]
    public void Dispose_DisposesSyncControls()
    {
        var container = new ScopedControlContainer();
        var disposable = new SyncDisposableControl();

        container.Register(disposable);

        container.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Dispose_ThrowsOnAsyncDisposable()
    {
        var container = new ScopedControlContainer();
        var asyncDisposable = new AsyncDisposableControl();

        container.Register(asyncDisposable);

        Assert.Throws<InvalidOperationException>(() => container.Dispose());
    }

    [Fact]
    public async Task DisposeFloatingControlsAsync_DisposesOnlyFloatingControls()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedControlContainer>();
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var container = scope.ServiceProvider.GetRequiredService<ScopedControlContainer>();

        var page = new TestPage(scope.ServiceProvider);

        var inPageControl = new SyncDisposableControl();
        page.Controls.AddWithoutPageEvents(inPageControl);

        var floatingControl = new SyncDisposableControl();
        container.Register(floatingControl);

        await container.DisposeFloatingControlsAsync();

        Assert.False(inPageControl.IsDisposed, "Control in page should not be disposed");
        Assert.True(floatingControl.IsDisposed, "Floating control should be disposed");
    }

    [Fact]
    public async Task DisposeFloatingControlsAsync_WaitsForRegisteredTasks()
    {
        var container = new ScopedControlContainer();
        var tcs = new TaskCompletionSource();
        var taskWasCompletedOnDispose = false;

        container.RegisterTask(tcs.Task.ContinueWith(_ => taskWasCompletedOnDispose = true));

        var disposeTask = container.DisposeFloatingControlsAsync().AsTask();

        Assert.False(taskWasCompletedOnDispose);

        tcs.SetResult();
        await disposeTask;

        Assert.True(taskWasCompletedOnDispose);
    }

    private class SyncDisposableControl : Control, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private class AsyncDisposableControl : Control, IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

    private class TestPage : Page
    {
        private readonly IServiceProvider _serviceProvider;

        public TestPage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var context = Substitute.For<HttpContext>();
            context.RequestServices.Returns(_serviceProvider);
            ((IInternalPage)this).SetContext(context);
        }

        protected override IServiceProvider ServiceProvider => _serviceProvider;
    }
}