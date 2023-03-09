using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Events;
using WebFormsCore.Security;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public class Page : Control, INamingContainer, IStateContainer, System.Web.UI.Page
{
    private HttpContext? _context;
    private IServiceProvider? _serviceProvider;
    private ScopedControlContainer? _scopedContainer;

    public Page()
    {
        ClientScript = new ClientScriptManager(this);
    }

    public bool EnablePageViewState { get; set; } = true;

    public Csp Csp { get; set; } = new();

    public ClientScriptManager ClientScript { get; }

    protected override HttpContext Context => _context ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; set; }

    protected override IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not available.");

    private ScopedControlContainer ScopedContainer => _scopedContainer ??= _serviceProvider?.GetRequiredService<ScopedControlContainer>() ?? throw new InvalidOperationException("Scoped control container not available.");

    public List<HtmlForm> Forms { get; set; } = new();

    public override HtmlForm? Form => null;

    internal HtmlForm? ActiveForm { get; set; }

    public object GetDataItem() => throw new NotImplementedException();

    internal List<IBodyControl> BodyControls { get; set; } = new();

    internal async Task<HtmlForm?> ProcessRequestAsync(CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

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

        var isPost = Context.Request.IsMethod("POST");
        var form = await viewStateManager.LoadAsync(Context, this);

        ActiveForm = form;

        if (form != null)
        {
            Forms.RemoveAll(i => i != form && i.Parent.Controls.Remove(i));
        }
        else if (isPost)
        {
            Forms.RemoveAll(i => i.Parent.Controls.Remove(i));
        }

        await InvokeLoadAsync(token, form);

        if (isPost)
        {
            var eventTarget = Context.Request.Form["__EVENTTARGET"];

            if (!string.IsNullOrEmpty(eventTarget))
            {
                var eventArgument = Context.Request.Form["__EVENTARGUMENT"];
                await InvokePostbackAsync(token, form, eventTarget, eventArgument);
            }

            if (form != null)
            {
                await form.OnSubmitAsync(token);
            }
        }

        await InvokePreRenderAsync(token, form);

        return form;
    }

    public void Initialize(IServiceProvider provider, HttpContext context)
    {
        _serviceProvider = provider;
        _context = context;
    }

    protected internal virtual void RegisterDisposable(Control control)
    {
        ScopedContainer.Register(control);
    }
}
