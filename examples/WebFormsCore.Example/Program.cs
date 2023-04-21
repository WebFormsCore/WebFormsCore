using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using WebFormsCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();

var app = builder.Build();

app.UseStack(stack =>
{
    stack.UseWebFormsCore();
});

app.Run();
