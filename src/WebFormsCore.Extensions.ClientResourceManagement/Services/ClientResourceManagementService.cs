using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Events;
using WebFormsCore.Features;
using WebFormsCore.Providers;
using WebFormsCore.UI;

namespace WebFormsCore.Services;

internal class ClientResourceManagementService : PageService
{
    public override async ValueTask BeforePreRenderAsync(Page page, CancellationToken token)
    {
        var service = page.Context.RequestServices.GetService<ClientDependencyCollection>();

        if (service is null or { Files.Count: 0 })
        {
            return;
        }

        var registeredCssNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var registeredJsNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var type = typeof(ClientResourceManagementService);
        var pathProviders = page.Context.RequestServices.GetServices<IClientDependencyPathProvider>();

        var clientScript = page.ClientScript;
        var files = service.Files
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Name);

        foreach (var file in files)
        {
            if (file is { Name: null } and { FilePath: null })
            {
                continue;
            }

            // Check if the name has already been registered
            if (file.Name != null)
            {
                var registeredNames = file.DependencyType switch
                {
                    ClientDependencyType.Css => registeredCssNames,
                    ClientDependencyType.Javascript => registeredJsNames,
                    _ => throw new InvalidOperationException()
                };

                if (!registeredNames.Add(file.Name))
                {
                    continue;
                }
            }

            // Resolve the path
            var path = file.FilePath;

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var provider in pathProviders)
            {
                var resolvedPath = await provider.GetPathAsync(page, file, token);

                if (resolvedPath != null)
                {
                    path = resolvedPath;
                    break;
                }
            }

            if (path is null)
            {
                throw new InvalidOperationException($"Could not resolve path for {file.DependencyType.ToString().ToLowerInvariant()}-file {file.Name ?? file.FilePath}");
            }

            // Register the file to the page
            switch (file.DependencyType)
            {
                case ClientDependencyType.Css:
                    clientScript.RegisterStartupStyleLink(type, file.Name ?? path, path, attributes: file.Attributes);
                    break;
                case ClientDependencyType.Javascript:
                    clientScript.RegisterStartupScript(type, file.Name ?? path, path, attributes: file.Attributes);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
