using WebFormsCore.UI;

namespace WebFormsCore.Tests.Pages;

public class DynamicControl : Control
{
    protected override async Task OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        await Controls.AddAsync(
            new DisposableControl()
        );
    }
}
