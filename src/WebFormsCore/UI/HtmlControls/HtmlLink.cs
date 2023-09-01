using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlLink : HtmlContainerControl
{
    protected override bool GenerateAutomaticID => false;
    
    public HtmlLink()
        : base("link")
    {
    }

    protected override Task OnPreRenderAsync(CancellationToken token)
    {
        if (Attributes["rel"] == "stylesheet" && Uri.TryCreate(Attributes["href"], UriKind.Absolute, out var href))
        {
            Page.Csp.StyleSrc.SourceList.Add($"{href.Scheme}://{href.Host}");
        }

        return Task.CompletedTask;
    }
}
