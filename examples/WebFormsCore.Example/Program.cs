using System.Threading.Tasks;
using HttpStack;
using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Example;
using WebFormsCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms(b =>
{
    b.AddControlCompiler();
    b.AddGridCellRenderers();
    b.AddClientResourceManagement();
});
// a B
builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    options.HiddenClass = "d-none";
    options.DisabledClass = "disabled";
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

app.UseSession();
app.UseWebSockets();

app.UseStack(stack =>
{
    stack.RunPath("/favicon.ico", context =>
    {
        context.Response.StatusCode = 404;
        return Task.CompletedTask;
    });

    stack.UseWebFormsCore();

    stack.UsePage();
    stack.RunPage<Default>();
});

app.Run();
