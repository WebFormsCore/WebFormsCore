# Getting Started with WebForms Core

This guide will help you set up your first WebForms Core project.

## Prerequisites

- .NET 10.0 SDK or higher
- Visual Studio, VS Code or Rider

## Using the Template (Recommended)

The fastest way to get started is using the project template:

```bash
dotnet new install WebFormsCore.Templates
dotnet new wfc -n MyWebFormsApp
cd MyWebFormsApp
dotnet run
```

This creates a project with a `Program.cs`, `Default.aspx` page, `web.config` for tag prefix registration, and all the necessary configuration.

## Manual Installation

If you prefer to set up the project manually:

### 1. Create a new project

Create a new ASP.NET Core Empty project:

```bash
dotnet new web -n MyWebFormsApp
cd MyWebFormsApp
```

### 2. Configure the project file

Change your `.csproj` to use the `WebFormsCore.SDK.AspNetCore` SDK. This SDK includes all the necessary MSBuild logic to compile `.aspx` and `.ascx` files.

```xml
<Project Sdk="WebFormsCore.SDK.AspNetCore/0.0.1-alpha.81">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

</Project>
```

### 3. Create a web.config

WebForms Core uses `web.config` to register tag prefixes for server controls. Create a `web.config` file in the project root:

```xml
<configuration>
  <system.web>
    <pages>
      <controls>
        <add tagPrefix="asp" namespace="WebFormsCore.UI.WebControls" />
        <add tagPrefix="asp" namespace="WebFormsCore.UI.HtmlControls" />
      </controls>
    </pages>
  </system.web>
</configuration>
```

This registers the `asp` tag prefix so you can use controls like `<asp:Button>`, `<asp:TextBox>`, etc. in your `.aspx` files. You can use any prefix you prefer (e.g., `wfc`).

### 4. Set up Program.cs

Register WebForms Core in your `Program.cs` file.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add WebForms Core services
builder.Services.AddWebFormsCore();

var app = builder.Build();

// Enable WebForms Core middleware (static files, etc.)
app.UseWebFormsCore();

// Map pages from the assembly
app.MapPages();

app.Run();
```

### 5. Create your first Page

Create a file named `Default.aspx`:

```aspx
<%@ Page Language="C#" Route="/" CodeBehind="Default.aspx.cs" Inherits="MyWebFormsApp.Default" %>

<!DOCTYPE html>
<html>
<head>
    <title>Hello WebForms Core</title>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Hello World!</h1>
        <asp:Literal ID="litMessage" runat="server" />
        <asp:Button ID="btnClick" runat="server" Text="Click Me" OnClick="btnClick_Click" />
    </form>
</body>
</html>
```

Create the code-behind file `Default.aspx.cs`:

```csharp
namespace MyWebFormsApp;

public partial class Default : WebFormsCore.UI.Page
{
    protected async Task btnClick_Click(object? sender, EventArgs e)
    {
        litMessage.Text = "You clicked the button at " + DateTime.Now.ToString();
    }
}
```

> **Important:** The class must be `partial` because the source generator creates a companion file that wires up controls declared in the `.aspx` markup (like `litMessage` and `btnClick`).

## Running the application

Run the application using `dotnet run`. The console output will show which URL the application is listening on (e.g., `http://localhost:5136`). Navigate to that URL in your browser to see your first page!

## What's Next?

- [Routing](fundamentals/routing.md) — Learn how routes are defined and how to use parameters.
- [Lifecycle](fundamentals/lifecycle.md) — Understand the control lifecycle and async methods.
- [Standard Controls](components/standard-controls.md) — See all available built-in controls.
- [ViewState](fundamentals/viewstate.md) — Learn how state is preserved across postbacks.
