using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.MasterPages.Pages;

public partial class FindControlPage : Page
{
    public string? MasterLabelText { get; private set; }
    public bool FoundControlInMaster { get; private set; }
    public bool HasHeader { get; private set; }

    protected override ValueTask OnLoadAsync(CancellationToken token)
    {
        // Test accessing master page controls
        MasterLabelText = Master.SiteTitle;

        // Test FindControl across boundaries
        FoundControlInMaster = Master.FindControl("lblSiteTitle") is not null;

        // Test Page.Header integration
        HasHeader = Header is not null;

        return base.OnLoadAsync(token);
    }
}
