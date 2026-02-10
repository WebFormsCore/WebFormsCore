using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public record UserModel(string Name, string Email, string Role);

public record ProductModel(string Product, string Price);

public partial class SkeletonRepeaterTest : Page
{
    private static readonly List<UserModel> Users =
    [
        new("Alice", "alice@example.com", "Admin"),
        new("Bob", "bob@example.com", "Editor"),
        new("Charlie", "charlie@example.com", "Viewer"),
        new("Diana", "diana@example.com", "Editor"),
        new("Eve", "eve@example.com", "Admin")
    ];

    private static readonly List<ProductModel> Products =
    [
        new("Widget", "$9.99"),
        new("Gadget", "$24.50"),
        new("Doohickey", "$4.75")
    ];

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await base.OnInitAsync(token);

        lazyRepeater.ContentLoaded += OnLazyRepeaterLoaded;
        lazyGrid.ContentLoaded += OnLazyGridLoaded;
    }

    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);

        if (!IsPostBack)
        {
            staticGrid.SetDataSource(Products);
            await staticGrid.DataBindAsync(token);
        }
    }

    private async Task OnLazyRepeaterLoaded(LazyLoader sender, EventArgs e)
    {
        await Task.Delay(2000); // Simulate slow data fetch

        repeater.SetDataSource(Users);
        await repeater.DataBindAsync();
    }

    private async Task OnLazyGridLoaded(LazyLoader sender, EventArgs e)
    {
        await Task.Delay(2000); // Simulate slow data fetch

        grid.SetDataSource(Users);
        await grid.DataBindAsync();
    }

    protected Task repeater_OnItemDataBound(Repeater sender, RepeaterItemEventArgs e)
    {
        if (e.Item.DataItem is UserModel user)
        {
            e.Item.FindControl<Label>("lblName")!.Text = user.Name;
            e.Item.FindControl<Label>("lblRole")!.Text = user.Role;
        }

        return Task.CompletedTask;
    }

    protected Task btnToggle_OnClick(Button sender, EventArgs e)
    {
        skeletonContainer.Loading = !skeletonContainer.Loading;
        sender.Text = skeletonContainer.Loading ? "Toggle Loading" : "Toggle Skeleton";
        return Task.CompletedTask;
    }

}
