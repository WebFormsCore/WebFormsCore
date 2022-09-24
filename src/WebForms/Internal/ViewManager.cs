﻿#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using WebFormsCore.Compiler;

namespace WebFormsCore;

internal class ViewManager : IDisposable
{
    private readonly ConcurrentDictionary<string, PageEntry> _pages = new();
    private readonly List<FileSystemWatcher> _watchers;
    private readonly IWebFormsEnvironment _environment;
    private readonly ILogger<ViewManager> _logger;

    public ViewManager(IWebFormsEnvironment environment, ILogger<ViewManager> logger)
    {
        _environment = environment;
        _logger = logger;
        _watchers = new List<FileSystemWatcher>();

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

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var modifyTime = File.GetLastWriteTimeUtc(e.FullPath);

        if (!TryGetPath(e.FullPath, out var path) ||
            !_pages.TryGetValue(path, out var entry) ||
            entry.LastModified >= modifyTime)
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

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        if (!fullPath.StartsWith(_environment.ContentRootPath))
        {
            path = null;
            return false;
        }

        path = fullPath.Substring(_environment.ContentRootPath.Length).TrimStart('\\', '/');
        return true;
    }


    public async ValueTask<Type> GetTypeAsync(string path)
    {
        var entry = _pages.GetOrAdd(path, p => new PageEntry(p));

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
        var entry = _pages.GetOrAdd(path, p => new PageEntry(p));

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

    private Type UpdateType(string path, PageEntry entry)
    {
        if (entry.Type != null)
        {
            return entry.Type;
        }

        entry.Type = CompilePage(path);
        entry.LastModified = File.GetLastWriteTimeUtc(entry.Path);
        entry.NextCheck = DateTimeOffset.Now.AddSeconds(5);

        return entry.Type;
    }

    private Type CompilePage(string path)
    {
        var fullPath = Path.Combine(_environment.ContentRootPath, path);

        var sw = Stopwatch.StartNew();
        var (compilation, typeName, designerType) = ViewCompiler.Compile(fullPath);

        Type? type = null;

        var assemblyName = designerType.Type?.ContainingAssembly.Name;

        if (assemblyName != null)
        {
            type = Type.GetType($"{designerType.Namespace}.CompiledViews+{designerType.Name}View, {assemblyName}");

            if (type != null)
            {
                var attribute = type.GetCustomAttribute<CompiledViewAttribute>();

                if (attribute?.Hash == designerType.Hash)
                {
                    _logger.LogDebug("Using pre-compiled view of page {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);
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

            _logger.LogDebug("Compiled view of page {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);
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
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }
}
