using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class ControlLifecycleTest
{
    private class LifecycleControl : Control
    {
        public bool InitCalled { get; set; }
        public bool LoadCalled { get; set; }

        protected override ValueTask OnInitAsync(CancellationToken ct)
        {
            InitCalled = true;
            return base.OnInitAsync(ct);
        }

        protected override ValueTask OnLoadAsync(CancellationToken ct)
        {
            LoadCalled = true;
            return base.OnLoadAsync(ct);
        }
        
        public void SetState(ControlState state) => _state = state;
        public ControlState GetState() => _state;
    }

    private Page CreatePage()
    {
        var services = new ServiceCollection();
        services.AddWebFormsCore();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var page = new Page();
        ((IInternalPage)page).SetContext(httpContext);
        return page;
    }

    [Fact]
    public async Task Add_CatchUp_Lifecycle()
    {
        var page = CreatePage();
        var parent = new LifecycleControl();
        page.Controls.AddWithoutPageEvents(parent);
        parent.SetState(ControlState.Loaded);

        var child = new LifecycleControl();
        
        await parent.Controls.AddAsync(child);

        Assert.True(child.InitCalled);
        Assert.True(child.LoadCalled);
        Assert.Equal(ControlState.Loaded, child.GetState());
    }

    [Fact]
    public async Task Add_CatchUp_PreRender()
    {
        var page = CreatePage();
        var parent = new LifecycleControl();
        page.Controls.AddWithoutPageEvents(parent);
        parent.SetState(ControlState.PreRendered);

        var child = new LifecycleControl();
        
        await parent.Controls.AddAsync(child);

        Assert.True(child.InitCalled);
        Assert.True(child.LoadCalled);
        // PreRender is usually triggered by Page.Render. 
        // Let's see if AddAsync catches up to PreRendered.
        Assert.Equal(ControlState.PreRendered, child.GetState());
    }

    [Fact]
    public async Task Add_NoCatchUp_IfParentConstructed()
    {
        var parent = new LifecycleControl();
        // Default is Constructed

        var child = new LifecycleControl();
        
        await parent.Controls.AddAsync(child);

        Assert.False(child.InitCalled);
        Assert.False(child.LoadCalled);
        Assert.Equal(ControlState.Constructed, child.GetState());
    }

    [Fact]
    public async Task Add_CatchUp_MultipleLevels()
    {
        var page = CreatePage();
        var root = new LifecycleControl();
        page.Controls.AddWithoutPageEvents(root);
        root.SetState(ControlState.Loaded);

        var parent = new LifecycleControl();
        var child = new LifecycleControl();

        // Adding parent to root (catch up parent)
        await root.Controls.AddAsync(parent);
        Assert.True(parent.InitCalled);
        Assert.True(parent.LoadCalled);

        // Adding child to parent (catch up child)
        await parent.Controls.AddAsync(child);
        Assert.True(child.InitCalled);
        Assert.True(child.LoadCalled);
    }
}
