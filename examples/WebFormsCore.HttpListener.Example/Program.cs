using System;
using HttpStack;
using HttpStack.NetHttpListener;
using WebFormsCore;
using WebFormsCore.Example;

using var listener = new System.Net.HttpListener();

listener.Prefixes.Add("http://localhost:5000/");

var builder = new HttpApplicationBuilder();
builder.Services.AddWebFormsCore();

var app = builder.Build();
app.Run(context => context.ExecutePageAsync<Default>());

listener.Start(app);

Console.WriteLine("Listening on http://localhost:5000/");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
