using Microsoft.AspNetCore.Builder;
using WebFormsCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();

#if WFC_COMPILER
builder.Services.AddWebFormsCompiler();
#endif

var app = builder.Build();

app.MapAspx("/", "Default.aspx");
app.MapFallbackToAspx();

app.Run();
