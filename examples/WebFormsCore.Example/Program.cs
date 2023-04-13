using HttpStack.AspNetCore;
using Microsoft.AspNetCore.Builder;
using WebFormsCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();

#if WFC_COMPILER
builder.Services.AddWebFormsCompiler();
#endif

var app = builder.Build();

app.UseStack(stack =>
{
    stack.UseWebFormsCore();
});

app.Run();
