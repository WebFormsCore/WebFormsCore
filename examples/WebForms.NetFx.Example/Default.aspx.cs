using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebForms.NetFx.Example
{
    public partial class Default : Page
    {
        protected override ValueTask OnLoadAsync(CancellationToken token)
        {
            if (IsPostBack)
            {
                litText.Text += "Hello ";
            }

            return default;
        }
    }
}