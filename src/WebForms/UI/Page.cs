using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Microsoft.Extensions.DependencyInjection;

namespace System.Web.UI;

public class Page : Control, INamingContainer
{
    private IHttpContext? _context;
    
    protected override IHttpContext Context => _context ??= HttpContextAccessor.Current ?? throw new InvalidOperationException("No HttpContext available.");

    public bool IsPostBack { get; set; }

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
}
