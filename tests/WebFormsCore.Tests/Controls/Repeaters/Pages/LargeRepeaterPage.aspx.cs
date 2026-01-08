using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters.Pages;

public partial class LargeRepeaterPage : Page
{
    public record RepeaterDataItem(int Id);

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        if (!IsPostBack)
        {
            await rptItems.DataBindAsync(token);
        }
    }

    protected Task rptItems_OnItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e.Item.ItemType is ListItemType.Item or ListItemType.AlternatingItem)
        {
            var lblName = (Label)e.Item.FindControl("lblName")!;
            var btnSetId = (LinkButton)e.Item.FindControl("btnSetId")!;
            var divContainer = (HtmlGenericControl)e.Item.FindControl("container")!;
            var item = (RepeaterDataItem)e.Item.DataItem!;
            var id = item.Id.ToString();

            lblName.Text = Guid.NewGuid().ToString();

            btnSetId.CommandArgument = id;
            divContainer.Attributes["data-id"] = id;
        }

        return Task.CompletedTask;
    }

    public string? SelectedId { get; set; }

    protected Task btnSetId_OnClick(LinkButton sender, EventArgs e)
    {
        SelectedId = sender.CommandArgument;

        if (sender.FindParent<RepeaterItem>()?.DataItem is not RepeaterDataItem item ||
            item.Id.ToString() != SelectedId)
        {
            throw new InvalidOperationException("Invalid selected item.");
        }

        return Task.CompletedTask;
    }

    protected Task rptItems_OnNeedDataSource(UI.WebControls.Repeater sender, NeedDataSourceEventArgs e)
    {
        sender.SetDataSource(Enumerable.Range(0, 100).Select(i => new RepeaterDataItem(i)));
        return Task.CompletedTask;
    }
}
