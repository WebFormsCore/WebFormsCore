using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebForms.Example;

public partial class Default : Page
{
    protected override ValueTask OnLoadAsync(CancellationToken token)
    {
        if (IsPostBack)
        {
            litText.Text += "Hello ";
            litText2.Text += "Hello ";
        }

        return default;
    }
}
