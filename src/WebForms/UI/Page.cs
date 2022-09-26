using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Security;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;
using HttpContext = System.Web.HttpContext;

namespace WebFormsCore.UI;

public class Page : Control, INamingContainer
{
    private HttpContext? _context;
    private IServiceProvider? _serviceProvider;

    public Csp Csp { get; set; } = new();

    protected override HttpContext Context => _context ??= HttpContext.Current ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; set; }

    protected override IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not available.");

    public List<HtmlForm> Forms { get; set; } = new();

    public override HtmlForm? Form => Forms.FirstOrDefault(i => i.Global);

    public async Task<Control> ProcessRequestAsync(CancellationToken token)
    {
        var viewStateManager = ServiceProvider.GetRequiredService<IViewStateManager>();

        InvokeFrameworkInit(token);
        await InvokeInitAsync(token);

        Control target = this;

        var form = await viewStateManager.LoadAsync(Context, this);

        if (form is { Global: false })
        {
            target = form;
        }

        await target.InvokeLoadAsync(token, form);

        if (form != null)
        {
            var eventTarget = Context.Request.Form["__EVENTTARGET"];
            var eventArgument = Context.Request.Form["__EVENTARGUMENT"];

            await target.InvokePostbackAsync(token, form, eventTarget, eventArgument);
        }

        await target.InvokePreRenderAsync(token, form);

        return target;
    }

    public void Initialize(IServiceProvider provider, HttpContext context)
    {
        _serviceProvider = provider;
        _context = context;
    }
}
