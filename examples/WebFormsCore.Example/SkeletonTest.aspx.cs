using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class SkeletonTest : Page
{
    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        lazyContent.ContentLoaded += OnLazyContentLoaded;
    }

    private async Task OnLazyContentLoaded(LazyLoader sender, EventArgs e)
    {
        await Task.Delay(2000); // Simulate loading delay
        lblLazyTitle.Text = "Content loaded successfully!";
        lblLazyTime.Text = $"Loaded at: {DateTime.Now:HH:mm:ss.fff}";
    }

    protected Task btnToggle_OnClick(Button sender, EventArgs e)
    {
        skeleton.Loading = !skeleton.Loading;
        sender.Text = skeleton.Loading ? "Toggle Loading" : "Toggle Skeleton";
        return Task.CompletedTask;
    }
}
