using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.ViewState;

public class ViewStateManagerThreadSafetyTests
{
    private static IViewStateManager CreateViewStateManager(string? encryptionKey = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddWebFormsCore();

        if (encryptionKey != null)
        {
            services.Configure<ViewStateOptions>(o => o.EncryptionKey = encryptionKey);
        }

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IViewStateManager>();
    }

    private static (Control Control, Page Page) CreateControlWithViewState()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var page = new Page();
        ((IInternalPage)page).SetContext(httpContext);

        var label = new Label { ID = "lbl", Text = "Test" };
        page.Controls.AddWithoutPageEvents(label);

        return (page, page);
    }

    [Fact]
    public async Task WhenConcurrentWritesThenNoException()
    {
        var viewStateManager = CreateViewStateManager();

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            var (control, _) = CreateControlWithViewState();
            using var owner = await viewStateManager.WriteAsync(control, out _);
        }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task WhenConcurrentWritesWithHmacKeyThenNoException()
    {
        var viewStateManager = CreateViewStateManager(encryptionKey: "test-encryption-key-for-hmac");

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            var (control, _) = CreateControlWithViewState();
            using var owner = await viewStateManager.WriteAsync(control, out _);
        }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task WhenConcurrentWriteAndLoadThenNoException()
    {
        var viewStateManager = CreateViewStateManager();

        // First, write a viewstate to use for loading
        var (control, _) = CreateControlWithViewState();
        using var initialOwner = await viewStateManager.WriteAsync(control, out var length);
        var viewStateBase64 = System.Text.Encoding.UTF8.GetString(initialOwner.Memory.Span.Slice(0, length));

        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(async () =>
        {
            if (i % 2 == 0)
            {
                // Write
                var (ctrl, _) = CreateControlWithViewState();
                using var owner = await viewStateManager.WriteAsync(ctrl, out _);
            }
            else
            {
                // Load
                var (ctrl, _) = CreateControlWithViewState();
                await viewStateManager.LoadAsync(ctrl, viewStateBase64);
            }
        }));

        await Task.WhenAll(tasks);
    }
}
