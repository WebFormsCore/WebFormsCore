using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI.WebControls;
using HttpContext = System.Web.HttpContext;

namespace WebFormsCore.UI;

public class Page : Control, INamingContainer
{
    private HttpContext? _context;
    private IServiceProvider? _serviceProvider;

    protected override HttpContext Context => _context ??= HttpContext.Current ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; set; }

    protected override IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not available.");

    public List<HtmlForm> Forms { get; set; } = new();

    public override HtmlForm? Form => Forms.FirstOrDefault(i => i.Global);

    public async Task<Control> ProcessRequestAsync(CancellationToken token)
    {
        InvokeFrameworkInit(token);
        await InvokeInitAsync(token);

        Control target = this;

        var request = Context.Request;
        var method = request.HttpMethod;

        HtmlForm? form = null;
        
        if (method == "POST")
        {
            IsPostBack = true;

            var formId = request.Form["__FORM"];
            var viewState = request.Form["__VIEWSTATE"];
            form = Forms.FirstOrDefault(i => i.UniqueID == formId);

            if (form != null && viewState != null)
            {
                form.LoadViewState(viewState);

                if (!form.Global)
                {
                    target = form;
                }
            }
        }

        await InvokeLoadAsync(token, form);

        return target;
    }

    public void SetServiceProvider(IServiceProvider provider)
    {
        _serviceProvider = provider;
    }
}
