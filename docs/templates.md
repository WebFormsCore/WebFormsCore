# Templates

WebForms Core provides `dotnet new` templates for quickly scaffolding projects and files.

## Installation

Install the template package from NuGet:

```bash
dotnet new install WebFormsCore.Templates
```

## Project Templates

### Web Application (`wfc`)

Creates a new WebForms Core web application with a sample page, `Program.cs`, and `web.config`.

```bash
dotnet new wfc -n MyWebFormsApp
cd MyWebFormsApp
dotnet run
```

The generated project includes:

| File | Description |
|---|---|
| `Program.cs` | ASP.NET Core host with `AddWebFormsCore()`, `UseWebFormsCore()`, and `MapPages()` |
| `Default.aspx` | Sample page with a button and literal control |
| `Default.aspx.cs` | Code-behind with a click handler |
| `web.config` | Tag prefix registration for `wfc` |
| `.csproj` | Project file using `WebFormsCore.SDK.AspNetCore` |

### Test Project (`wfc-tests`)

Creates a test project for browser-based integration testing of WebForms Core controls using xUnit v3 and Selenium.

```bash
dotnet new wfc-tests -n MyWebFormsApp.Tests
```

The generated project includes:

| File | Description |
|---|---|
| `Assembly.cs` | Registers `SeleniumFixture` as an xUnit assembly fixture |
| `Usings.cs` | Common `using` statements and `[assembly: WebFormsAssembly]` |
| `ButtonClickTest.cs` | Sample test that clicks a button and asserts the result |
| `Pages/ButtonClickPage.aspx` | Test page with a button and label |
| `Pages/ButtonClickPage.aspx.cs` | Code-behind for the test page |
| `web.config` | Tag prefix registration |

See [Testing](advanced/testing.md) for a full guide on writing tests.

## Item Templates

Item templates create individual files that you add to an existing project.

### Page (`wfc-page`)

Creates an `.aspx` page with a code-behind file.

```bash
dotnet new wfc-page -n MyPage
```

This creates `MyPage.aspx` and `MyPage.aspx.cs`. The code-behind inherits from `WebFormsCore.UI.Page` and includes a `[Route]` attribute.

### User Control (`wfc-control`)

Creates an `.ascx` user control with a code-behind file.

```bash
dotnet new wfc-control -n MyControl
```

This creates `MyControl.ascx` and `MyControl.ascx.cs`. The code-behind inherits from `WebFormsCore.UI.Control`.

### Master Page (`wfc-master`)

Creates a `.master` master page with a code-behind file.

```bash
dotnet new wfc-master -n Site
```

This creates `Site.master` and `Site.master.cs`. The master page includes `ContentPlaceHolder` controls for `head` and `main`.

## Customizing the Output

All item templates respect the `--output` / `-o` flag to specify the directory:

```bash
dotnet new wfc-page -n OrderDetails -o Pages
dotnet new wfc-control -n NavMenu -o Controls
dotnet new wfc-master -n Site -o MasterPages
```

Namespaces are automatically inferred from the output directory relative to the project root.

## Uninstalling

To remove the templates:

```bash
dotnet new uninstall WebFormsCore.Templates
```
