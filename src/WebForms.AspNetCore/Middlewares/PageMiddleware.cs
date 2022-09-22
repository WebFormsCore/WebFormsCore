using System.Reflection;
using System.Web.Compiler;
using System.Web.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Emit;
using HttpContextAccessor = System.Web.HttpContextAccessor;

namespace WebForms.AspNetCore.Middlewares;

public class PageMiddleware
{
    private readonly RequestDelegate _next;
    private Type? _type;

    public PageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        HttpContextAccessor.Current = new CoreHttpContext(
            context,
            context.RequestServices,
            context.RequestAborted
        );

        if (_type == null)
        {
            var (compilation, typeName) = PageCompiler.Compile(@"C:\Sources\WebForms\examples\WebForms.Example\Default.aspx");

            using var assemblyStream = new MemoryStream();
            using var symbolsStream = new MemoryStream();

            var emitOptions = new EmitOptions();

            var result = compilation.Emit(
                peStream: assemblyStream,
                pdbStream: symbolsStream,
                options: emitOptions);


            if (result.Success)
            {
                var assembly = Assembly.Load(assemblyStream.ToArray(), symbolsStream.ToArray());
                _type = assembly.GetType(typeName)!;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        var page = (Page) Activator.CreateInstance(_type)!;
        var cancellationToken = context.RequestAborted;
        var control = await page.ProcessRequestAsync(cancellationToken);

        await using var textWriter = new StreamWriter(context.Response.Body);
        await using var writer = new HtmlTextWriter(textWriter, context.Response.Body);

        context.Response.ContentType = "text/html";
        await control.RenderAsync(writer, cancellationToken);
        await writer.FlushAsync();


    }

}
