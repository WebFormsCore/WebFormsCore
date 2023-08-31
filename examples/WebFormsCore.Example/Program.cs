using HttpStack;
using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using WebFormsCore;
using WebFormsCore.Example;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();

var app = builder.Build();

app.UseWebSockets();

var stack = app.UseStack();

stack.UseWebFormsCore();

stack.Run(async context =>
{
    await context.ExecutePageAsync<Default>();
});

app.Run();
