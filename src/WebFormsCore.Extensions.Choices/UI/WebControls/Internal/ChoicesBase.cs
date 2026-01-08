using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls.Internal;

[ParseChildren(true)]
public abstract class ChoicesBase : Control
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        Page.ClientScript.RegisterStartupStyleLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/styles/choices.min.css");
        Page.ClientScript.RegisterStartupDeferStaticScript(typeof(Choices), "/js/choices.min.js", Resources.Script);
    }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        await base.OnPreRenderAsync(token);

        if (Page.Csp.Enabled)
        {
            var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>();

            if (options?.Value.HiddenClass is null) Page.Csp.StyleSrc.AddUnsafeInlineHash("display:none;");
            Page.Csp.ImgSrc.Add("data:");
        }
    }
}
