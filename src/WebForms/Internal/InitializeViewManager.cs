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
    private readonly ViewManager _viewManager;
    private readonly IWebFormsEnvironment _environment;
    private readonly ILogger<InitializeViewManager> _logger;

    public InitializeViewManager(ViewManager viewManager, IWebFormsEnvironment environment, ILogger<InitializeViewManager> logger)
    {
        _viewManager = viewManager;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var files = Directory.GetFiles(_environment.ContentRootPath, "*.*", SearchOption.AllDirectories)
            .Where(i => Path.GetExtension((string?)i) is ".aspx" or ".ascx");

#if NET
        await Parallel.ForEachAsync(files, stoppingToken, async (fullPath, _) =>
        {
#else
        foreach (var fullPath in files)
        {
#endif
            if (!_viewManager.TryGetPath(fullPath, out var path)) return;

            try
            {
                await _viewManager.GetTypeAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-compile page {Path}", path);
            }
#if NET
        });
#else
        }
#endif
    }
}
