using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore;
using WebFormsCore.UI;

namespace WebForms.Example.Controls;

public partial class Counter : Control
{
    [ViewState] private int _count;

    protected override ValueTask OnLoadAsync(CancellationToken token)
    {
        if (Context.Request.HttpMethod == "POST" && Context.Request.Form["__EVENTTARGET"] == btnIncrement.UniqueID)
        {
            _count++;

            rptItems.DataSource = Enumerable.Range(0, _count).Select(i => $"Item {i}");
            rptItems.DataBind();
        }

        tbPrefix.Text = tbPrefix.Text?.ToUpperInvariant();
        litValue.Text = $"Count: {tbPrefix.Text}{_count}";

        return default;
    }
}
