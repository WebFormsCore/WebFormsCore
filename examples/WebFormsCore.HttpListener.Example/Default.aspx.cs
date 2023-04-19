using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int PostbackCount { get; set; }

    protected override Task OnLoadAsync(CancellationToken token)
    {
        if (Page.IsPostBack)
        {
            PostbackCount++;
        }

        return base.OnLoadAsync(token);
    }
}
