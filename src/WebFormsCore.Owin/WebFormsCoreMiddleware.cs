using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Web;
using HttpMultipartParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebFormsCore.Implementation;

namespace WebFormsCore;

using AppFunc = Func<IDictionary<string, object>, Task>;

public class WebFormsCoreMiddleware
{
    private readonly AppFunc _next;
    private readonly IServiceProvider _serviceProvider;

    public WebFormsCoreMiddleware(AppFunc next)
    {
        var services = new ServiceCollection();
        services.UseOwinWebForms();
        _serviceProvider = services.BuildServiceProvider();
        _next = next;
    }

    public WebFormsCoreMiddleware(AppFunc next, IServiceCollection services)
    {
        _serviceProvider = services.BuildServiceProvider();
        _next = next;
    }

    public WebFormsCoreMiddleware(AppFunc next, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _next = next;
    }

    public async Task Invoke(IDictionary<string, object> env)
    {
        if (env["owin.RequestPath"] is not string envPath)
        {
            await _next(env);
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var application = scope.ServiceProvider.GetRequiredService<IWebFormsApplication>();
        var path = application.GetPath(envPath);

        if (path == null)
        {
            await _next(env);
            return;
        }

        var context = new HttpContextImpl(); // TODO: Pooling
        context.SetHttpContext(env, scope.ServiceProvider);

        if (context.Request is {Method: "POST", ContentType: { } contentType})
        {
            var stream = context.Request.Body;

            if (contentType.Contains("multipart/form-data"))
            {
                var parser = await MultipartFormDataParser.ParseAsync(stream);
                var dictionary = new Dictionary<string, StringValues>();

                foreach (var parameter in parser.Parameters)
                {
                    dictionary[parameter.Name] = parameter.Data;
                }

                context.Request.Form = dictionary;
            }
            else if (contentType.Contains("application/x-www-form-urlencoded"))
            {
                string input;

                using (var reader = new StreamReader(stream))
                {
                    input = await reader.ReadToEndAsync();
                }

                var dictionary = new Dictionary<string, StringValues>();
                var coll = HttpUtility.ParseQueryString(input);

                foreach (var key in coll.AllKeys)
                {
                    dictionary[key] = coll.GetValues(key);
                }

                context.Request.Form = dictionary;
            }
        }

        await application.ProcessAsync(context, path, context.RequestAborted);
    }
}
