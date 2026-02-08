using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.MasterPages.Pages;

public partial class MasterTypeContentPage : Page
{
    protected override ValueTask OnLoadAsync(CancellationToken token)
    {
        // Access strongly-typed Master property - this only compiles if @MasterType works
        lblTitle.Text = Master.SiteTitle;
        return base.OnLoadAsync(token);
    }
}
