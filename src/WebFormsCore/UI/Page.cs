using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebFormsCore.Events;
using WebFormsCore.Security;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

[ParseChildren(false)]
public class Page : Control, INamingContainer, IStateContainer, System.Web.UI.Page, IInternalPage
{
    private IHttpContext? _context;
    private ScopedControlContainer? _scopedContainer;

    internal List<object>? ChangedPostDataConsumers;

    public Page()
    {
        ClientScript = new ClientScriptManager(this);
    }

    public HtmlHead? Header { get; internal set; }

    public HtmlBody? Body { get; internal set; }

    public bool EnablePageViewState { get; set; } = true;

    public override bool EnableViewState { get; set; } = true;

    public Csp Csp { get; set; } = new();

    public ClientScriptManager ClientScript { get; }

    public StreamPanel? ActiveStreamPanel { get; set; }

    protected override IHttpContext Context => _context ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; internal set; }

    public bool IsExternal { get; internal set; }

    public bool IsStreaming => ActiveStreamPanel != null;

    protected override IServiceProvider ServiceProvider => Context.RequestServices;

    private ScopedControlContainer ScopedContainer => _scopedContainer ??= ServiceProvider.GetRequiredService<ScopedControlContainer>();

    public List<HtmlForm> Forms { get; set; } = new();

    public override HtmlForm? Form => null;

    internal HtmlForm? ActiveForm { get; set; }

    internal List<IBodyControl> BodyControls { get; set; } = new();

    internal async Task InitAsync(CancellationToken token)
    {
        IsPostBack = string.Equals(Context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);

        InvokeFrameworkInit(token);

        foreach (var pageService in ServiceProvider.GetServices<IPageService>())
        {
            await pageService.BeforeInitializeAsync(this);
        }

        await InvokeInitAsync(token);

        foreach (var pageService in ServiceProvider.GetServices<IPageService>())
        {
            await pageService.AfterInitializeAsync(this);
        }
    }

    private async ValueTask<HtmlForm?> LoadViewStateAsync()
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        if (!viewStateManager.EnableViewState)
        {
            return null;
        }

        var request = Context.Request;
        var isPostback = request.Method == "POST";

        if (!isPostback)
        {
            return null;
        }

        HtmlForm? form = null;
        StringValues formState = default;

        if (request.Form.TryGetValue("wfcForm", out var formId) &&
            request.Form.TryGetValue("wfcFormState", out formState))
        {
            form = Forms.FirstOrDefault(i => i.UniqueID == formId);
        }

        if (form is null or { UpdatePage: true} &&
            request.Form.TryGetValue("wfcPageState", out var pageState))
        {
            await viewStateManager.LoadAsync(this, pageState.ToString()!);
        }

        if (form != null && !string.IsNullOrEmpty(formState))
        {
            await viewStateManager.LoadAsync(form, formState.ToString()!);
        }

        return form;
    }

    internal async Task<Control> ProcessRequestAsync(CancellationToken token)
    {
        var form = await LoadViewStateAsync();
        var target = form is null or { UpdatePage: true } ? (Control)this : form;

        ActiveForm = form;

        await target.InvokeLoadAsync(token, form);

        if (IsPostBack)
        {
            if (Context.Request.Form.TryGetValue("wfcTarget", out var eventTarget))
            {
                var eventArgument = Context.Request.Form.TryGetValue("wfcArgument", out var eventArgumentValue)
                    ? eventArgumentValue.ToString()
                    : string.Empty;

                await target.InvokePostbackAsync(token, form, eventTarget, eventArgument);
            }

            await RaiseChangedEventsAsync(token);

            if (form != null)
            {
                await form.OnSubmitAsync(token);
            }
        }

        await target.InvokePreRenderAsync(token, form);

        return target;
    }

    public async ValueTask RaiseChangedEventsAsync(CancellationToken cancellationToken)
    {
        if (ChangedPostDataConsumers is not {} consumers) return;

        foreach (var consumer in consumers)
        {
            if (consumer is IPostBackAsyncDataHandler eventHandler)
            {
                await eventHandler.RaisePostDataChangedEventAsync(cancellationToken);
            }
            else if (consumer is IPostBackDataHandler handler)
            {
                handler.RaisePostDataChangedEvent();
            }
        }
    }

    internal void ClearChangedPostDataConsumers()
    {
        ChangedPostDataConsumers?.Clear();
    }

    protected internal virtual void Initialize(IHttpContext context)
    {
        _context = context;
    }

    void IInternalPage.Initialize(IHttpContext context) => Initialize(context);

    protected internal virtual void RegisterDisposable(Control control)
    {
        ScopedContainer.Register(control);
    }

    protected override string GetUniqueIDPrefix() => "p$";
}
