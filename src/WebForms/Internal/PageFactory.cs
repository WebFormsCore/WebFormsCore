#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using WebFormsCore.Compiler;

namespace WebFormsCore;

internal class PageFactory : IDisposable
{
    private readonly ConcurrentDictionary<string, PageEntry> _pages = new();
    private readonly FileSystemWatcher _watcher;
    private readonly ILogger<PageFactory> _logger;

    public PageFactory(IWebFormsEnvironment environment, ILogger<PageFactory> logger)
    {
        _logger = logger;
        _watcher = new FileSystemWatcher(environment.ContentRootPath, "*.aspx")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var modifyTime = File.GetLastWriteTimeUtc(e.FullPath);

        if (!_pages.TryGetValue(e.FullPath, out var entry) || entry.LastModified >= modifyTime)
        {
            return;
        }

        entry.LastModified = modifyTime;
        entry.Type = null;

        _ = Task.Run(async () =>
        {
            try
            {
                await GetTypeAsync(e.FullPath).AsTask();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-compile page {Path}", e.FullPath);
            }
        });
    }

    public async ValueTask<Type> GetTypeAsync(string path)
    {
        var entry = _pages.GetOrAdd(path, p => new PageEntry(p));

        if (entry.Type != null)
        {
            return entry.Type;
        }

        await entry.Lock.WaitAsync();

        try
        {
            if (entry.Type == null)
            {
                entry.Type = CompilePage(path);
                entry.LastModified = File.GetLastWriteTimeUtc(entry.Path);
                entry.NextCheck = DateTimeOffset.Now.AddSeconds(5);
            }
        }
        finally
        {
            entry.Lock.Release();
        }

        return entry.Type;
    }

    private Type CompilePage(string path)
    {
        var sw = Stopwatch.StartNew();
        var (compilation, typeName, designerType) = PageCompiler.Compile(path);

        Type? type = null;

        var assemblyName = designerType.Type?.ContainingAssembly.Name;

        if (assemblyName != null)
        {
            type = Type.GetType($"{designerType.Namespace}.{designerType.Name}+CompiledView, {assemblyName}");

            if (type != null)
            {
                var attribute = type.GetCustomAttribute<CompiledViewAttribute>();

                if (attribute?.Hash == designerType.Hash)
                {
                    _logger.LogDebug("Using pre-compiled view of page {Path}, time spend: {Time}ms", path, sw.ElapsedMilliseconds);
                    return type;
                }

                type = null;
            }
        }

        if (type == null)
        {
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
            type = assembly.GetType(typeName)!;

            _logger.LogDebug("Compiled view of page {Path}, time spend: {Time}ms", path, sw.ElapsedMilliseconds);
        }

        return type;
    }

    private class PageEntry
    {
        public PageEntry(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public Type? Type { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public DateTimeOffset? NextCheck { get; set; }

        public SemaphoreSlim Lock { get; } = new(1, 1);
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
