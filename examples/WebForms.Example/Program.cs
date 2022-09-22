using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebForms();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapRazorPages();
    endpoints.MapBlazorHub();
});

app.UseWebForms();
app.Run();

