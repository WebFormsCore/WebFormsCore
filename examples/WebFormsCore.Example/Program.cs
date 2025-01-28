using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Example;
using WebFormsCore.Options;
using WebFormsCore.UI;

var builder = WebApplication.CreateBuilder(args);

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

app.Use((c, n) =>
{
    if (c.Request.Path == "/favicon.ico")
    {
        c.Response.StatusCode = 404;
        return Task.CompletedTask;
    }

    return n(c);
});

app.UseSession();
app.UseWebSockets();

app.UseWebFormsCore();

app.UsePage();
app.RunPage<Default>();

app.Run();
