using WebFormsCore.UI;

namespace WebFormsCore.Providers;

public class PrefixClientDependencyPathProvider<T> : IClientDependencyPathProvider
{
    private readonly Func<T, string> _getter;

    public PrefixClientDependencyPathProvider(string alias, Func<T, string> getter)
    {
        Alias = alias;
        _getter = getter;
    }

    public string Alias { get; }

    public ValueTask<string?> GetPathAsync(Page page, IClientDependencyFile file, CancellationToken token)
    {
        if (file.PathNameAlias == null
            || page is not T typedPage
            || !file.PathNameAlias.Equals(Alias, StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        var path = file.FilePath;

        if (path is null)
        {
            return default;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            path = path.Substring(2);
        }

        var prefix = _getter(typedPage);

        if (string.IsNullOrEmpty(prefix))
        {
            return default;
        }

        var lastChar = prefix[prefix.Length - 1];
        string result;

        if (lastChar is '/' or '\\')
        {
            result = prefix + path;
        }
        else
        {
            result = prefix + '/' + path;
        }

        return new ValueTask<string?>(result);
    }
}
