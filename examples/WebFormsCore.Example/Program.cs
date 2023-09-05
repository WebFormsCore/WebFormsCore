using System.Threading.Tasks;
using HttpStack;
using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Example;
using WebFormsCore.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();
builder.Services.AddSingleton<IGridCellRenderer, CheckBoxCellRenderer>();
builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    options.HiddenClass = "d-none";
});

var app = builder.Build();

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
