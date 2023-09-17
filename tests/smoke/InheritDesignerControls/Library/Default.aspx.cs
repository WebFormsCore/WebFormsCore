using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace Library;

public partial class Default : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        if (Body != null)
        {
            await Body.Controls.AddAsync(
                LoadControl("Control1.ascx")
            );
        }
    }
}
