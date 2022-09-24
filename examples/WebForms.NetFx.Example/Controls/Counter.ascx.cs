using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebForms.Example.Controls
{
    public partial class Counter : Control
    {
        [ViewState] private int _count;

        protected override ValueTask OnPreRenderAsync(CancellationToken token)
        {
            tbPrefix.Text = tbPrefix.Text?.ToUpperInvariant();
            litValue.Text = $"Count: {tbPrefix.Text}{_count}";

            return default;
        }

        protected async ValueTask btnIncrement_OnClick(object sender, EventArgs e)
        {
            _count++;

            rptItems.DataSource = Enumerable.Range(0, _count).Select(i => $"Item {i}");
            await rptItems.DataBindAsync();
        }

        protected ValueTask rptItems_OnItemDataBound(object sender, RepeaterItem<string> e)
        {
            var item = (Literal) e.FindControl("litItem");

            if (item != null)
            {
                item.Text = e.DataItem;
            }

            return default;
        }
    }
}
