using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Security;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

public class Page : Control, INamingContainer, IStateContainer, System.Web.UI.Page
{
    private HttpContext? _context;
    private IServiceProvider? _serviceProvider;

    public Page()
    {
        ClientScript = new ClientScriptManager(this);
    }

    public Csp Csp { get; set; } = new();

    public ClientScriptManager ClientScript { get; }

    protected override HttpContext Context => _context ??= HttpContext.Current ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; set; }

    protected override IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not available.");

    public List<HtmlForm> Forms { get; set; } = new();

    public override HtmlForm? Form => null;

    internal async Task<HtmlForm?> ProcessRequestAsync(CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        InvokeFrameworkInit(token);
        await InvokeInitAsync(token);

        var form = await viewStateManager.LoadAsync(Context, this);

        if (form != null)
        {
            Forms.RemoveAll(i => i != form && i.Parent.Controls.Remove(i));
        }

        await InvokeLoadAsync(token, form);

        if (form != null)
        {
            var eventTarget = Context.Request.Form["__EVENTTARGET"];
            var eventArgument = Context.Request.Form["__EVENTARGUMENT"];

            await InvokePostbackAsync(token, form, eventTarget, eventArgument);
        }

        await InvokePreRenderAsync(token, form);

        return form;
    }

    public void Initialize(IServiceProvider provider, HttpContext context)
    {
        _serviceProvider = provider;
        _context = context;
    }
}
