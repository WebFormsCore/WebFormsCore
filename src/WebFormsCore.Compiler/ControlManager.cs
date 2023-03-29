#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using WebFormsCore.Compiler;
using WebFormsCore.Nodes;

namespace WebFormsCore;

public class ControlManager : IDisposable, IControlManager
{
    private readonly ConcurrentDictionary<string, ControlEntry> _controls = new();
    private readonly List<FileSystemWatcher> _watchers;
    private readonly IWebFormsEnvironment _environment;
    private readonly ILogger<ControlManager> _logger;
    private readonly Dictionary<string, Type> _compiledViews;

    public ControlManager(IWebFormsEnvironment environment, ILogger<ControlManager> logger)
    {
        _environment = environment;
        _logger = logger;
        _watchers = new List<FileSystemWatcher>();
        _compiledViews = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
            .GetCustomAttributes<AssemblyViewAttribute>()
            .ToDictionary(x => x.Path, x => x.Type, StringComparer.OrdinalIgnoreCase);

        if (environment is { EnableControlWatcher: true, ContentRootPath: not null })
        {
            foreach (var extension in new[] { "*.aspx", "*.ascx" })
            {
                var watcher = new FileSystemWatcher(environment.ContentRootPath, extension)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnChanged;

                _watchers.Add(watcher);
            }
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var modifyTime = File.GetLastWriteTimeUtc(e.FullPath);

        if (!TryGetPath(e.FullPath, out var path)) return;
        if (!_controls.TryGetValue(path, out var entry)) return;
        if (entry.LastModified >= modifyTime) return;

        entry.LastModified = modifyTime;
        entry.Type = null;

        _ = Task.Run(async () =>
        {
            try
            {
                await GetTypeAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-compile control {Path}", e.FullPath);
            }
        });
    }

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        if (_environment.ContentRootPath is null || !fullPath.StartsWith(_environment.ContentRootPath))
        {
            path = null;
            return false;
        }

        path = DefaultControlManager.NormalizePath(
            fullPath.Substring(_environment.ContentRootPath.Length).TrimStart('\\', '/')
        );
        return true;
    }

    public async ValueTask<Type> GetTypeAsync(string path)
    {
        var entry = _controls.GetOrAdd(path, p => new ControlEntry(p));

        if (entry.Type != null) return entry.Type;

        await entry.Lock.WaitAsync();

        try
        {
            return UpdateType(path, entry);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public Type GetType(string path)
    {
        var entry = _controls.GetOrAdd(path, p => new ControlEntry(p));

        if (entry.Type != null) return entry.Type;

        entry.Lock.Wait();

        try
        {
            return UpdateType(path, entry);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private Type UpdateType(string path, ControlEntry entry)
    {
        if (entry.Type != null)
        {
            return entry.Type;
        }

        entry.Type = CompileControl(path);
        entry.LastModified = File.GetLastWriteTimeUtc(entry.Path);
        entry.NextCheck = DateTimeOffset.Now.AddSeconds(5);

        return entry.Type;
    }

    private Type CompileControl(string path)
    {
        var sw = Stopwatch.StartNew();

        if (!_compiledViews.TryGetValue(path, out var type))
        {
            type = null;
        }

        if (_environment.ContentRootPath is null)
        {
            throw new InvalidOperationException("ContentRootPath is not set");
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, path);
        var text = File.ReadAllText(fullPath).ReplaceLineEndings("\n");

        if (type != null)
        {
            var attribute = type.GetCustomAttribute<CompiledViewAttribute>();

            if (attribute?.Hash == RootNode.GenerateHash(text))
            {
                _logger.LogDebug("Using pre-compiled view of control {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);
                return type;
            }

            type = null;
        }

        var (compilation, designerType) = ViewCompiler.Compile(fullPath, text);

        if (type != null)
        {
            return type;
        }

        using var assemblyStream = new MemoryStream();

        var emitOptions = new EmitOptions();

        var result = compilation.Emit(
            peStream: assemblyStream,
            options: emitOptions);

        if (!result.Success)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                _logger.LogError("Compilation error: {Message}", diagnostic.ToString());
            }

            throw new InvalidOperationException();
        }

        var assembly = Assembly.Load(assemblyStream.ToArray());
        type = assembly.GetType(designerType.DesignerFullTypeName!)!;

        _logger.LogDebug("Compiled view of page {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);

        return type;
    }

    private class ControlEntry
    {
        public ControlEntry(string path)
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
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }
}
