using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using Default = Library.Default;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebFormsCore(config =>
{
    config.AddControlCompiler();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

app.UseSession();
app.UseWebSockets();

app.RunPage<Default>();

app.Run();
