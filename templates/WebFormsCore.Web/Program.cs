var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebFormsCore();

var app = builder.Build();

app.UseWebFormsCore();
app.MapPages();

app.Run();
