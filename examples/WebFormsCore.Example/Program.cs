using System.Threading.Tasks;
using HttpStack;
using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Example;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms(b =>
{
    b.AddGridCellRenderers();
});

builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    options.HiddenClass = "d-none";
});

builder.Services.Configure<ViewStateOptions>(options =>
{
    options.EncryptionKey = builder.Configuration.GetValue<string>("ViewState:EncryptionKey");
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

app.UseSession();
app.UseWebSockets();

var stack = app.UseStack();

stack.RunPath("/favicon.ico", context =>
{
    context.Response.StatusCode = 404;
    return Task.CompletedTask;
});

stack.UseWebFormsCore();

stack.Run(async context =>
{
    await context.ExecutePageAsync<Default>();
});

app.Run();
