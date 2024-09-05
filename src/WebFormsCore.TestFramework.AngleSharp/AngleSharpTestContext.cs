using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;


public interface IAngleSharpTestContext : ITestContext
{
    ValueTask PostbackAsync(string selector, string? argument = null);

    ValueTask PostbackAsync(AngleSharp.Dom.IElement element, string? argument = null);

    ValueTask PostbackAsync(Control? control = null, string? argument = null);
}

public sealed class AngleSharpTestContext<T> : ITestContext<T>, IAngleSharpTestContext
    where T : Page
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Func<IPageManager, HttpContext, Task<T>> _create;
    private AsyncServiceScope? _lastScope;
    private IDocument? _document;
    private readonly IBrowsingContext _browsingContext;

    public AngleSharpTestContext(ServiceProvider serviceProvider, Func<IPageManager, HttpContext, Task<T>> create)
    {
        _serviceProvider = serviceProvider;
        _create = create;

        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
    }

    public T Control { get; private set; } = null!;

    public string Html { get; private set; } = null!;

    public IDocument Document => _document ?? throw new InvalidOperationException("Document not set");

    public HttpResponse Response { get; private set; } = null!;

    public ValueTask GetAsync()
    {
        return DoRequestAsync(request =>
        {
            request.Method = "GET";
            return default;
        });
    }

    public ValueTask PostbackAsync(string selector, string? argument = null)
    {
        var element = Document.QuerySelector(selector);

        if (element == null)
        {
            throw new InvalidOperationException("Element not found");
        }

        return PostbackAsync(element, argument);
    }

    public ValueTask PostbackAsync(AngleSharp.Dom.IElement element, string? argument = null)
    {
        var id = element.Id;
        var name = element.GetAttribute("name");

        foreach (var control in Control.EnumerateControls())
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

    public ValueTask PostbackAsync(Control? control = null, string? argument = null)
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

            request.Method = "POST";
            request.Form = new FormCollection(form);
            return default;
        });
    }

    private async ValueTask DoRequestAsync(Func<HttpRequest, ValueTask>? prepareRequest = null)
    {

        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Protocol = "http",
                IsHttps = false,
                Path = "/",
                Headers =
                {
                    Host = "localhost"
                }
            }
        };

        if (prepareRequest != null) await prepareRequest.Invoke(context.Request);

        _document?.Dispose();

        if (_lastScope != null)
        {
            await _lastScope.Value.DisposeAsync();
        }

        await using var stream = new MemoryStream();
        var scope = _serviceProvider.CreateAsyncScope();

        context.RequestServices = scope.ServiceProvider;
        context.Response.Body = stream;

        var pageManager = scope.ServiceProvider.GetRequiredService<IPageManager>();
        Control = await _create(pageManager, context);

        stream.Position = 0;

        Html = Encoding.UTF8.GetString(stream.ToArray());
        Response = context.Response;
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

    public ValueTask<string> GetHtmlAsync()
    {
        return new ValueTask<string>(Html);
    }

    public ValueTask ReloadAsync()
    {
        return GetAsync();
    }

    public IElement? GetElementById(string id)
    {
        var element = Document.GetElementById(id);
        return element == null ? null : new AngleSharpElement(this, element);
    }

    public IElement? QuerySelector(string selector)
    {
        var result = Document.QuerySelector(selector);
        return result == null ? null : new AngleSharpElement(this, result);
    }

    public IElement[] QuerySelectorAll(string selector)
    {
        return Document.QuerySelectorAll(selector)
            .Select(element => (IElement) new AngleSharpElement(this, element))
            .ToArray();
    }
}
