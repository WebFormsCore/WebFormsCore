using WebFormsCore.NativeAOT.Example.Context;

var services = new ServiceCollection();

services.AddOptions();
services.AddWebFormsInternals();
services.AddSingleton<IControlManager, DefaultControlManager>();

await using var provider = services.BuildServiceProvider();

var pageManager = provider.GetRequiredService<IPageManager>();

await using var scope = provider.CreateAsyncScope();
var consoleContext = new ConsoleContext(scope.ServiceProvider);

await pageManager.RenderPageAsync(consoleContext, "Test.aspx", CancellationToken.None);
