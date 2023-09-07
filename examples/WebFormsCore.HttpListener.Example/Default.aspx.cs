using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int Counter { get; set; }

    protected Task OnClick(Button sender, EventArgs e)
    {
        Counter++;
        return Task.CompletedTask;
    }
}
