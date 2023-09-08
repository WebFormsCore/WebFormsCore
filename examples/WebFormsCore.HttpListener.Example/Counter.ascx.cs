using System;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class Counter : Control
{
    [ViewState] public int Value { get; set; }

    protected Task increment_OnClick(Button sender, EventArgs e)
    {
        Value++;
        return Task.CompletedTask;
    }
}