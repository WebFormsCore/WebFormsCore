using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls;

public class RemovedControlUnloadTest
{
    [Fact]
    public void RemovedControlInternal_CompletedTask_DoesNotRegisterTask()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedControlContainer>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var page = new TestPage(scope.ServiceProvider);
        var parent = new TestControl();
        page.Controls.AddWithoutPageEvents(parent);

        var child = new Control();
        parent.Controls.AddWithoutPageEvents(child);

        parent.CallRemovedControlInternal(child);

        var container = scope.ServiceProvider.GetRequiredService<ScopedControlContainer>();
        Assert.Empty(container.UnloadedTasks);
    }

    [Fact]
    public void RemovedControlInternal_UncompletedTask_RegistersTask()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedControlContainer>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var page = new TestPage(scope.ServiceProvider);
        var parent = new TestControl();
        page.Controls.AddWithoutPageEvents(parent);

        var tcs = new TaskCompletionSource();
        var child = new AsyncUnloadControl(tcs.Task);
        parent.Controls.AddWithoutPageEvents(child);

        parent.CallRemovedControlInternal(child);

        var container = scope.ServiceProvider.GetRequiredService<ScopedControlContainer>();
        Assert.Single(container.UnloadedTasks);
        var registeredTask = container.UnloadedTasks.First();

        Assert.False(registeredTask.IsCompleted);

        tcs.SetResult();
        Assert.True(registeredTask.IsCompleted);
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

    private class TestControl : Control
    {
        public void CallRemovedControlInternal(Control control)
        {
            RemovedControlInternal(control);
        }
    }

    private class AsyncUnloadControl(Task task) : Control
    {
        protected override async ValueTask OnUnloadAsync(CancellationToken token)
        {
            await base.OnUnloadAsync(token);
            await task;
        }
    }
}
