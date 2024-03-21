#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebFormsCore;

internal class InitializeViewManager : BackgroundService
{
    private readonly IControlManager _controlManager;
    private readonly IWebFormsEnvironment _environment;
    private readonly ILogger<InitializeViewManager>? _logger;

    public InitializeViewManager(IControlManager controlManager, IWebFormsEnvironment environment, ILogger<InitializeViewManager>? logger = null)
    {
        _controlManager = controlManager;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.ContentRootPath is null)
        {
            return;
        }

        var files = Directory.GetFiles(_environment.ContentRootPath, "*.*", SearchOption.AllDirectories)
            .Where(i => Path.GetExtension(i) is ".aspx" or ".ascx");

        await Parallel.ForEachAsync(files, stoppingToken, async (fullPath, _) =>
        {
            if (_controlManager.TryGetPath(fullPath, out var path) &&
                !path.StartsWith("bin/") &&
                !path.StartsWith("obj/"))
            {
                try
                {
                    await _controlManager.GetTypeAsync(path);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to re-compile page {Path}", path);
                }
            }
        });
    }
}
