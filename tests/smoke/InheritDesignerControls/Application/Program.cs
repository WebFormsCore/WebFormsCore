using System.Threading.Tasks;
using HttpStack;
using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using Default = Application.Default;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms();

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

    stack.RunPage<Default>();
});

app.Run();
