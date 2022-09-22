using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis.Emit;
using WebFormsCore.Compiler;
using WebFormsCore.UI;

namespace WebFormsCore.Internal;

internal class WebFormsApplications : IWebFormsApplication
{
    private readonly IWebFormsEnvironment _environment;

    public WebFormsApplications(IWebFormsEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<bool> ProcessAsync(HttpContext context, IServiceProvider provider, CancellationToken token)
    {
        var path = Path.Combine(_environment.ContentRootPath, "Default.aspx");
        var page = CreatePage(path);

        page.SetServiceProvider(provider);

        var control = await page.ProcessRequestAsync(token);
        var stream = context.Response.OutputStream;

#if NETFRAMEWORK
        using var textWriter = new StreamWriter(stream);
        using var writer = new HtmlTextWriter(textWriter, stream);
#else
        await using var textWriter = new StreamWriter(stream);
        await using var writer = new HtmlTextWriter(textWriter, stream);
#endif

        context.Response.ContentType = "text/html";
        await control.RenderAsync(writer, token);
        await writer.FlushAsync();

        return true;
    }

    public static Page CreatePage(string path)
    {
        var rootPath = AppContext.BaseDirectory;

        var (compilation, typeName) = PageCompiler.Compile(path);

        using var assemblyStream = new MemoryStream();
        using var symbolsStream = new MemoryStream();

        var emitOptions = new EmitOptions();

        var result = compilation.Emit(
            peStream: assemblyStream,
            pdbStream: symbolsStream,
            options: emitOptions);

        if (!result.Success)
        {
            throw new InvalidOperationException();
        }

        var assembly = Assembly.Load(assemblyStream.ToArray(), symbolsStream.ToArray());
        var type = assembly.GetType(typeName)!;

        return (Page)Activator.CreateInstance(type)!;
    }
}