using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Example;
using WebFormsCore.Options;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(c => c.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddWebFormsCore(b =>
{
    b.AddControlCompiler();
    b.AddGridCellRenderers();
    b.AddClientResourceManagement();
});

builder.Services.Configure<ViewStateOptions>(options =>
{
    // options.Compact = false;
});

builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    options.EnableWebFormsPolyfill = true;
    options.HiddenClass = "d-none";
    options.DisabledClass = "disabled";
    options.DefaultScriptPosition = ScriptPosition.HeadEnd;
});

builder.Services.Configure<ViewStateOptions>(options =>
{
    options.EncryptionKey = builder.Configuration.GetValue<string>("ViewState:EncryptionKey");
});

builder.Services.Configure<TinyOptions>(options =>
{
    options.Branding = false;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

app.Use(async (c, n) =>
{
    if (c.Request.Path == "/favicon.ico")
    {
        c.Response.StatusCode = 404;
        return;
    }

    await n(c);
});

app.UseStaticFiles();

app.UseSession();
app.UseWebSockets();

app.UseWebFormsCore();

app.MapControl("/button", () =>
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

app.MapPage<Default>("/");

app.UsePage();

app.Run();
