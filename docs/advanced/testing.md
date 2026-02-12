# Testing

WebForms Core includes a testing framework for browser-based integration testing of controls and pages. Tests run against a real Kestrel server and use Selenium WebDriver to interact with the browser.

## Quick Start

Create a test project using the template:

```bash
dotnet new wfc-tests -n MyWebFormsApp.Tests
```

Or add the packages manually:

```xml
<PackageReference Include="WebFormsCore.TestFramework.Selenium" Version="0.0.1-alpha.81" />
<PackageReference Include="xunit.v3" Version="1.0.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
```

## Setup

### Assembly Fixture

Register `SeleniumFixture` as an xUnit assembly fixture so the browser is shared across all tests:

```csharp
[assembly: AssemblyFixture(typeof(SeleniumFixture))]
```

### Global Usings

A typical `Usings.cs` file:

```csharp
global using WebFormsCore;
global using WebFormsCore.Tests;
global using WebFormsCore.UI;
global using WebFormsCore.UI.HtmlControls;
global using WebFormsCore.UI.WebControls;
global using Xunit;

[assembly: WebFormsAssembly]
```

The `[WebFormsAssembly]` attribute ensures the source generator processes `.aspx` files in the test project.

## Writing Tests

### Page-Based Tests

The most common pattern uses `.aspx` pages as test fixtures. Create a page, then write a test that navigates to it and asserts the result.

**Pages/ButtonClickPage.aspx**:

```aspx
<%@ Page Language="C#" Inherits="MyTests.Pages.ButtonClickPage" %>
<form id="form1" runat="server">
    <asp:Label ID="lblResult" runat="server" Text="Not Clicked" />
    <asp:Button ID="btnClick" runat="server" Text="Click Me" OnClick="btnClick_Click" />
</form>
```

**Pages/ButtonClickPage.aspx.cs**:

```csharp
namespace MyTests.Pages;

public partial class ButtonClickPage : Page
{
    protected Task btnClick_Click(object? sender, EventArgs e)
    {
        lblResult.Text = "Clicked!";
        return Task.CompletedTask;
    }
}
```

**ButtonClickTest.cs**:

```csharp
public class ButtonClickTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClickButton(Browser type)
    {
        // Start a page by its type (resolved from the assembly)
        await using var result = await fixture.StartAsync<ButtonClickPage>(type);

        // Query the DOM
        var label = result.Browser.QuerySelector("#lblResult")!;
        Assert.Equal("Not Clicked", label.Text);

        // Interact with the page
        await result.Browser.QuerySelector("#btnClick")!.ClickAsync();
        Assert.Equal("Clicked!", label.Text);
    }
}
```

### Code-Only Tests (Inline Controls)

You can also write tests without `.aspx` files by building the control tree in code. This is ideal for testing individual controls in isolation.

```csharp
public class CounterTests(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task IncrementCounter(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var count = new Ref<int>();

            return new Panel
            {
                Controls =
                [
                    new Label
                    {
                        Controls =
                        [
                            new Literal(() => $"Count: {count.Value}")
                        ]
                    },
                    new Button
                    {
                        Text = "Increment",
                        OnClick = (_, _) => count.Value++,
                    }
                ]
            };
        });

        Assert.Equal("Count: 0", result.Browser.QuerySelector("span")?.Text);
        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Count: 1", result.Browser.QuerySelector("span")?.Text);
    }
}
```

See [Code-only UI](code-only-ui.md) for more details on the inline control API.

### Referencing Controls with `Ref<T>`

Use `Ref<T>` to get a reference to a control in your inline tree, so you can assert or modify it:

```csharp
[Theory, ClassData(typeof(BrowserData))]
public async Task ClickUpdatesLabel(Browser type)
{
    await using var result = await fixture.StartAsync(type, () =>
    {
        var label = new Ref<Label>();

        return new Panel
        {
            Controls =
            [
                new Label
                {
                    Ref = label,
                    Text = "Not clicked"
                },
                new Button
                {
                    Text = "Click me",
                    OnClick = (_, _) => label.Value.Text = "Clicked",
                }
            ]
        };
    });

    Assert.Equal("Not clicked", result.Browser.QuerySelector("span")?.Text);
    await result.Browser.QuerySelector("button")!.ClickAsync();
    Assert.Equal("Clicked", result.Browser.QuerySelector("span")?.Text);
}
```

## `SeleniumFixture` API

### `StartAsync` Overloads

| Overload | Description |
|---|---|
| `StartAsync<TControl>(browser)` | Starts a page/control by type (DI-activated). Returns `ITestContext<TControl>`. |
| `StartAsync(browser, () => control)` | Starts with an inline control factory. Returns `CurrentState<TControl>`. |
| `StartAsync(browser, parent => state)` | Starts with a factory that receives the parent control. Returns `CurrentState<TState>`. |
| `StartAsync(browser, async (parent) => state)` | Async version of the above. |

### `SeleniumFixtureOptions`

Pass options to customize the test host:

```csharp
var options = new SeleniumFixtureOptions
{
    EnableViewState = true,
    EnableWebSockets = true,
    Configure = services => services.AddSingleton<IMyService, MyService>(),
    ConfigureApp = app => app.UseStaticFiles()
};

await using var result = await fixture.StartAsync(type, () => new MyControl(), options);
```

### Browser Selection

`BrowserData` provides Chrome and Firefox test data for `[ClassData]`. Use `[Theory, ClassData(typeof(BrowserData))]` to run tests in all available browsers.

For combinatorial testing across multiple parameters:

```csharp
[Theory, CombinatorialData]
public async Task Test(Browser type, [CombinatorialValues(1, 5, 10)] int count)
{
    // Runs for each combination of browser × count
}
```

## Querying the DOM

The `result.Browser` object (of type `ISeleniumTestContext`) provides:

- `QuerySelector(selector)` — Find an element by CSS selector.
- `QuerySelectorAll(selector)` — Find all matching elements.
- `element.Text` — Get the visible text of an element.
- `element.GetAttributeAsync(name)` — Get an attribute value.
- `element.ClickAsync()` — Click an element (triggers a postback for server buttons).

## Running Tests

```bash
dotnet test
```

The framework automatically starts a Kestrel server on a random port with a self-signed HTTPS certificate. Chrome and Firefox drivers are resolved automatically.
