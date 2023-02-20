using Microsoft.AspNetCore.Builder;
using WebFormsCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();

var app = builder.Build();

app.MapAspx("/", "Default.aspx");
app.MapFallbackToAspx();

app.Run();
