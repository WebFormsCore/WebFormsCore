using WebFormsCore.UI;

namespace WebFormsCore.Tests.Pages;

public class DynamicControl : Control
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        await Controls.AddAsync(
            new DisposableControl()
        );
    }
}
