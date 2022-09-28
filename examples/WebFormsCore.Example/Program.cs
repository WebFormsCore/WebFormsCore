using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSystemWebAdapters();
builder.Services.AddWebForms();

var app = builder.Build();

app.UseSystemWebAdapters();
app.MapAspx("/", "Default.aspx");
app.MapFallbackToAspx();

app.Run();
