#nullable enable

using System;
using System.Collections;
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
#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace WebFormsCore;

public class ControlManager : IDisposable, IControlManager
{
    private readonly ConcurrentDictionary<string, ControlEntry> _controls;
    private readonly List<FileSystemWatcher> _watchers;
    private readonly IWebFormsEnvironment _environment;
    private readonly ILogger<ControlManager>? _logger;
    private readonly Dictionary<string, Type> _compiledViews;
    private readonly string? _contentRoot;

    public ControlManager(IControlTypeProvider provider, IWebFormsEnvironment environment, ILogger<ControlManager>? logger = null)
    {
        _environment = environment;
        _contentRoot = environment.ContentRootPath ?? AppContext.BaseDirectory;
        _logger = logger;
        _watchers = new List<FileSystemWatcher>();
        _compiledViews = provider.GetTypes();
        _controls = new ConcurrentDictionary<string, ControlEntry>();
        ViewTypes = new ControlsDictionary(_controls, _compiledViews);

        if (environment is { EnableControlWatcher: true, ContentRootPath: not null })
        {
            _logger?.LogDebug("Control watcher is enabled in {Path}", environment.ContentRootPath);

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

    public IReadOnlyDictionary<string, Type> ViewTypes { get; }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (!TryGetPath(e.FullPath, out var path)) return;

        if (_controls.TryGetValue(path, out var entry))
        {
            Update(entry, path);
        }

        foreach (var kv in _controls)
        {
            if (kv.Value.Includes.Contains(path))
            {
                Update(kv.Value, kv.Key, ignoreModifyDate: true);
            }
        }
    }

    private void Update(ControlEntry entry, string path, bool ignoreModifyDate = false)
    {
        var modifyTime = File.GetLastWriteTimeUtc(entry.Path);
        if (entry.LastModified >= modifyTime && !ignoreModifyDate) return;

        _logger?.LogDebug("Updating control {Path}", entry.Path);

        entry.LastModified = modifyTime;
        entry.IsCompiling = true;

#if NET6_0_OR_GREATER
        if (entry.LoadContext != null)
        {
            entry.LoadContext.Unload();
            entry.LoadContext = null;
        }
#endif

        _ = Task.Run(async () =>
        {
            const int maxAttempts = 4;

            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await GetTypeAsync(path, allowBackgroundCompile: false);
                }
                catch (IOException)
                {
                    _logger?.LogWarning("Could not read file {Path}, attempt {Attempt}/{MaxAttempts}", entry.Path, i, maxAttempts);
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to re-compile control {Path}", entry.Path);
                }
            }
        });
    }

    public bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path)
    {
        var current = fullPath;

        if (_contentRoot != null && current.StartsWith(_contentRoot))
        {
            current = current.Substring(_contentRoot.Length);
        }

        path = DefaultControlManager.NormalizePath(current);
        return _compiledViews.ContainsKey(path) || File.Exists(fullPath);
    }

    public ValueTask<Type> GetTypeAsync(string path)
    {
        return GetTypeAsync(path, allowBackgroundCompile: true);
    }

    public async ValueTask<Type> GetTypeAsync(string path, bool allowBackgroundCompile)
    {
        var normalizedPath = DefaultControlManager.NormalizePath(path);
        var entry = _controls.GetOrAdd(normalizedPath, p => new ControlEntry(p));

        if (entry.Type != null && (!entry.IsCompiling || _environment.CompileInBackground && allowBackgroundCompile))
        {
            return entry.Type;
        }

        await entry.Lock.WaitAsync();

        try
        {
            return UpdateType(normalizedPath, entry);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public Type GetType(string path)
    {
        var normalizedPath = DefaultControlManager.NormalizePath(path);
        var entry = _controls.GetOrAdd(normalizedPath, p => new ControlEntry(p));

        if (entry.Type != null && (!entry.IsCompiling || _environment.CompileInBackground))
        {
            return entry.Type;
        }

        entry.Lock.Wait();

        try
        {
            return UpdateType(normalizedPath, entry);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private Type UpdateType(string path, ControlEntry entry)
    {
        if (entry is { Type: not null, IsCompiling: false })
        {
            return entry.Type;
        }

        CompileControl(path, entry);
        entry.IsCompiling = false;

        if (entry.Type == null)
        {
            throw new FileNotFoundException($"Could not find type for path '{path}'");
        }

        entry.LastModified = File.GetLastWriteTimeUtc(entry.Path);
        entry.NextCheck = DateTimeOffset.Now.AddSeconds(5);

        entry.Includes.Clear();

        foreach (var include in entry.Type.GetCustomAttributes<CompiledViewInclude>())
        {
            entry.Includes.Add(include.Path);
        }

        return entry.Type;
    }

    private void CompileControl(string path, ControlEntry entry)
    {
        var sw = Stopwatch.StartNew();

        if (!_compiledViews.TryGetValue(path, out var type))
        {
            type = null;
        }

        if (_environment.ContentRootPath is null)
        {
            entry.Type = type;
            return;
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, path);

        if (!File.Exists(fullPath))
        {
            entry.Type = type;
            return;
        }

        using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().ReplaceLineEndings("\n");

        if (type != null)
        {
            var attribute = type.GetCustomAttribute<CompiledViewAttribute>();
            var isUpToDate = attribute?.Hash == RootNode.GenerateHash(text);

            if (isUpToDate)
            {
                foreach (var include in type.GetCustomAttributes<CompiledViewInclude>())
                {
                    var includeFullPath = Path.Combine(_environment.ContentRootPath, include.Path);

                    if (!File.Exists(includeFullPath))
                    {
                        isUpToDate = false;
                        break;
                    }

                    var includeText = File.ReadAllText(includeFullPath).ReplaceLineEndings("\n");

                    if (include.Hash != RootNode.GenerateHash(includeText))
                    {
                        isUpToDate = false;
                        break;
                    }
                }
            }

            if (isUpToDate)
            {
                _logger?.LogDebug("Using pre-compiled view of control {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);
                entry.Type = type;
                return;
            }
        }

        var (compilation, designerType) = ViewCompiler.Compile(
            fullPath,
            text,
            _environment.ContentRootPath);

        using var assemblyStream = new MemoryStream();

        var emitOptions = new EmitOptions();

        var result = compilation.Emit(
            peStream: assemblyStream,
            options: emitOptions);

        if (!result.Success)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                _logger?.LogError("Compilation error: {Message}", diagnostic.ToString());
            }

            throw new InvalidOperationException();
        }

#if NET6_0_OR_GREATER
        var loadContext = new AssemblyLoadContext("Control", isCollectible: true);

        assemblyStream.Seek(0, SeekOrigin.Begin);
        loadContext.LoadFromStream(assemblyStream);

        entry.LoadContext = loadContext;
        entry.Type = loadContext.Assemblies
            .SelectMany(i => i.GetTypes())
            .FirstOrDefault(i => i.FullName == designerType.DesignerFullTypeName);
#else
        var assembly = Assembly.Load(assemblyStream.ToArray());
        entry.Type = assembly.GetType(designerType.DesignerFullTypeName!)!;
#endif

        _logger?.LogDebug("Compiled view of page {Path}, time spend: {Time}ms", fullPath, sw.ElapsedMilliseconds);
    }

    private class ControlEntry
    {
        public ControlEntry(string path, Type? type = null)
        {
            Path = path;
            Type = type;
        }

        public string Path { get; }

        public Type? Type { get; set; }

        public bool IsCompiling { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public DateTimeOffset? NextCheck { get; set; }

        public SemaphoreSlim Lock { get; } = new(1, 1);

        public List<string> Includes { get; } = new();

#if NET6_0_OR_GREATER
        public AssemblyLoadContext? LoadContext { get; set; }
#endif
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }

    private class ControlsDictionary : IReadOnlyDictionary<string, Type>
    {
        private readonly ConcurrentDictionary<string, ControlEntry> _controls;
        private readonly Dictionary<string, Type> _compiledViews;

        public ControlsDictionary(
            ConcurrentDictionary<string, ControlEntry> controls,
            Dictionary<string, Type> compiledViews)
        {
            _controls = controls;
            _compiledViews = compiledViews;
        }

        public IEnumerator<KeyValuePair<string, Type>> GetEnumerator()
        {
            return _controls
                .Where(i => i.Value.Type != null)
                .Select(i => new KeyValuePair<string, Type>(i.Key, i.Value.Type!))
                .Concat(_compiledViews.Where(i => !_controls.ContainsKey(i.Key)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _controls.Count(i => i.Value.Type != null) +
                            _compiledViews.Count(i => !_controls.ContainsKey(i.Key));
        public bool ContainsKey(string key)
        {
            return _controls.ContainsKey(key) || _compiledViews.ContainsKey(key);
        }

        public bool TryGetValue(string key, out Type value)
        {
            if (_controls.TryGetValue(key, out var entry) && entry.Type != null)
            {
                value = entry.Type;
                return true;
            }

            if (_compiledViews.TryGetValue(key, out var type))
            {
                value = type;
                return true;
            }

            value = null!;
            return false;
        }

        public Type this[string key] => _controls.TryGetValue(key, out var entry) && entry.Type != null
            ? entry.Type
            : _compiledViews[key];

        public IEnumerable<string> Keys => _controls.Keys.Concat(_compiledViews.Keys).Distinct();

        public IEnumerable<Type> Values => _controls.Values
            .Where(i => i.Type != null).Select(i => i.Type!)
            .Concat(_compiledViews.Values.Where(i => !_controls.ContainsKey(i.FullName!)));
    }
}
