using WebFormsCore.UI;

namespace WebFormsCore.Tests.Events;

public class ControlEventsTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task EventsCalled(Browser type)
    {
        await using var result = await fixture.StartAsync<ControlEvents>(type);

        Assert.True(result.Control.PreInitCalled);
        Assert.True(result.Control.PreInitAsyncCalled);
        Assert.True(result.Control.PreInitEventCalled);

        Assert.True(result.Control.InitCalled);
        Assert.True(result.Control.InitAsyncCalled);
        Assert.True(result.Control.InitEventCalled);

        Assert.True(result.Control.LoadCalled);
        Assert.True(result.Control.LoadAsyncCalled);
        Assert.True(result.Control.LoadEventCalled);

        Assert.True(result.Control.PreRenderCalled);
        Assert.True(result.Control.PreRenderAsyncCalled);
        Assert.True(result.Control.PreRenderEventCalled);
    }

    private class ControlEvents : Control
    {
        public bool PreInitCalled { get; private set; }
        public bool PreInitAsyncCalled { get; private set; }
        public bool PreInitEventCalled { get; private set; }

        public bool InitCalled { get; private set; }
        public bool InitAsyncCalled { get; private set; }
        public bool InitEventCalled { get; private set; }

        public bool LoadCalled { get; private set; }
        public bool LoadAsyncCalled { get; private set; }
        public bool LoadEventCalled { get; private set; }

        public bool PreRenderCalled { get; private set; }
        public bool PreRenderAsyncCalled { get; private set; }
        public bool PreRenderEventCalled { get; private set; }

        public ControlEvents()
        {
            PreInit += (_, _) =>
            {
                PreInitEventCalled = true;
                return Task.CompletedTask;
            };

            Init += (_, _) =>
            {
                InitEventCalled = true;
                return Task.CompletedTask;
            };

            Load += (_, _) =>
            {
                LoadEventCalled = true;
                return Task.CompletedTask;
            };

            PreRender += (_, _) =>
            {
                PreRenderEventCalled = true;
                return Task.CompletedTask;
            };
        }

        protected override void OnPreInit(EventArgs args)
        {
            PreInitCalled = true;
            base.OnPreInit(args);
        }

        protected override ValueTask OnPreInitAsync(CancellationToken token)
        {
            PreInitAsyncCalled = true;
            return base.OnPreInitAsync(token);
        }

        protected override void OnInit(EventArgs args)
        {
            InitCalled = true;
            base.OnInit(args);
        }

        protected override ValueTask OnInitAsync(CancellationToken token)
        {
            InitAsyncCalled = true;
            return base.OnInitAsync(token);
        }

        protected override void OnLoad(EventArgs args)
        {
            LoadCalled = true;
            base.OnLoad(args);
        }

        protected override ValueTask OnLoadAsync(CancellationToken token)
        {
            LoadAsyncCalled = true;
            return base.OnLoadAsync(token);
        }

        protected override void OnPreRender(EventArgs args)
        {
            PreRenderCalled = true;
            base.OnPreRender(args);
        }

        protected override ValueTask OnPreRenderAsync(CancellationToken token)
        {
            PreRenderAsyncCalled = true;
            return base.OnPreRenderAsync(token);
        }
    }
}
