global using static WebFormsCore.Tests.TestUtils;

using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public sealed class TestResult<T> : IAsyncDisposable
    where T : Page
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Func<IPageManager, HttpContext, Task<T>> _create;
    private AsyncServiceScope? _lastScope;
    private IDocument? _document;
    private readonly IBrowsingContext _browsingContext;

    public TestResult(ServiceProvider serviceProvider, Func<IPageManager, HttpContext, Task<T>> create)
    {
        _serviceProvider = serviceProvider;
        _create = create;

        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
    }

    public T Page { get; private set; } = null!;

    public string Html { get; private set; } = null!;

    public IDocument Document => _document ?? throw new InvalidOperationException("Document not set");

    public HttpResponse Response { get; private set; } = null!;

    public Task GetAsync()
    {
        return DoRequestAsync(request =>
        {
            request.Method.Returns("GET");
            return default;
        });
    }

    public IElement GetElement(Control control)
    {
        return GetElement<IElement>(control);
    }

    public TElement GetElement<TElement>(Control control)
        where TElement : IElement
    {
        var id = control.ClientID;

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Control must have a ClientID", nameof(control));
        }

        var element = Document.GetElementById(id);

        if (element == null)
        {
            throw new ArgumentException($"Element with id '{id}' not found", nameof(control));
        }

        return (TElement) element;
    }

    public Task PostbackAsync(string selector, string? argument = null)
    {
        var element = Document.QuerySelector(selector);

        if (element == null)
        {
            throw new InvalidOperationException("Element not found");
        }

        var id = element.Id;
        var name = element.GetAttribute("name");

        foreach (var control in Page.EnumerateControls())
        {
            if (id != null && control.ClientID == id)
            {
                return PostbackAsync(control, argument);
            }

            if (name != null && control.UniqueID == name)
            {
                return PostbackAsync(control, argument);
            }
        }

        throw new ArgumentException($"Control with id '{id}' or name '{name}' not found", nameof(element));
    }

    public Task PostbackAsync(Control? control = null, string? argument = null)
    {
        return DoRequestAsync(request =>
        {
            var form = new Dictionary<string, StringValues>
            {
                ["wfcTarget"] = control?.UniqueID,
                ["wfcArgument"] = argument
            };

            var activeFormId = control?.Form?.ClientID;

            foreach (var element in Document.QuerySelectorAll("input, select, textarea"))
            {
                if (element.HasAttribute("data-wfc-ignore") || element.Closest("[data-wfc-ignore]") != null)
                {
                    continue;
                }

                var elementFormId = element.Closest("form")?.GetAttribute("id");

                if (elementFormId != null && elementFormId != activeFormId)
                {
                    continue;
                }

                switch (element)
                {
                    case IHtmlInputElement { Type: "submit", Name: not null } input:
                        form[input.Name] = new StringValues(input.Value);
                        break;
                    case IHtmlInputElement { Type: "checkbox" or "radio", Name: not null } input:
                        if (input.IsChecked)
                        {
                            form[input.Name] = new StringValues(input.Value);
                        }
                        break;
                    case IHtmlInputElement { Name: not null } input:
                        form[input.Name] = new StringValues(input.Value);
                        break;
                    case IHtmlSelectElement { Name: not null } select:
                        form[select.Name] = new StringValues(select.Value);
                        break;
                    case IHtmlTextAreaElement { Name: not null } textarea:
                        form[textarea.Name] = new StringValues(textarea.Value);
                        break;
                }
            }

            request.Method.Returns("POST");
            request.Form.Returns(new FormCollection(form));
            return default;
        });
    }

    private async Task DoRequestAsync(Func<HttpRequest, ValueTask>? prepareRequest = null)
    {
        var coreRequest = Substitute.For<HttpRequest>();
        if (prepareRequest != null) await prepareRequest.Invoke(coreRequest);

        _document?.Dispose();

        if (_lastScope != null)
        {
            await _lastScope.Value.DisposeAsync();
        }

        await using var stream = new MemoryStream();
        var scope = _serviceProvider.CreateAsyncScope();

        var coreResponse = Substitute.For<HttpResponse>();
        var responseHeaders = new HeaderDictionary();
        coreResponse.Headers.Returns(responseHeaders);
        coreResponse.Body.Returns(stream);

        var coreContext = Substitute.For<HttpContext>();
        coreContext.Request.Returns(coreRequest);
        coreContext.Response.Returns(coreResponse);
        coreContext.RequestServices.Returns(scope.ServiceProvider);

        var pageManager = scope.ServiceProvider.GetRequiredService<IPageManager>();
        Page = await _create(pageManager, coreContext);

        stream.Position = 0;

        Html = Encoding.UTF8.GetString(stream.ToArray());
        Response = coreResponse;
        _document = await _browsingContext.OpenAsync(req => req.Content(Html));

        _lastScope = scope;
    }

    public async ValueTask DisposeAsync()
    {
        _document?.Dispose();
        _browsingContext.Dispose();

        if (_lastScope != null)
        {
            await _lastScope.Value.DisposeAsync();
        }

        await _serviceProvider.DisposeAsync();
    }
}

public class TestEnvironment : IWebFormsEnvironment
{
    public string? ContentRootPath => AppContext.BaseDirectory;

    public bool EnableControlWatcher => false;

    public bool CompileInBackground => false;
}

internal static class TestUtils
{
    public static Task<TestResult<T>> RenderAsync<T>(bool enableViewState = true)
        where T : Page
    {
        return RenderAsync(async (pageManager, context) => (T) await pageManager.RenderPageAsync(context, typeof(T)), enableViewState);
    }

    public static Task<TestResult<Page>> RenderAsync(string path, bool enableViewState = true)
    {
        return RenderAsync(async (pageManager, context) => await pageManager.RenderPageAsync(context, path), enableViewState);
    }

    private static async Task<TestResult<T>> RenderAsync<T>(
        Func<IPageManager, HttpContext, Task<T>> create,
        bool enableViewState)
        where T : Page
    {
        var services = new ServiceCollection();

        services.AddWebFormsCore(builder =>
        {
            builder.Services.AddSingleton<IControlTypeProvider, AssemblyControlTypeProvider>();
        });

        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options =>
            {
                options.Enabled = enableViewState;
            });

        services.AddOptions<WebFormsCoreOptions>()
            .Configure(options =>
            {
                options.AddWebFormsCoreScript = false;
                options.AddWebFormsCoreHeadScript = false;
                options.EnableWebFormsPolyfill = false;
            });

        var serviceProvider = services.BuildServiceProvider();

        var result = new TestResult<T>(serviceProvider, create);

        await result.GetAsync();

        return result;
    }
}