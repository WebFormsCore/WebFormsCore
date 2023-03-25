using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private readonly Dictionary<string, StringValues> _form = new();
    private IDictionary<string, object> _env;
    private IReadOnlyDictionary<string, StringValues> _formValue;
    private IReadOnlyDictionary<string, StringValues> _query;

    public void SetHttpRequest(IDictionary<string, object> env)
    {
        _env = env;
        _formValue = null;
        _query = null;
    }

    public void Reset()
    {
        _form.Clear();
        _env = null!;
        _formValue = null;
        _query = null;
    }

    public string Method => _env["owin.RequestMethod"] as string;
    public string Scheme => _env["owin.RequestScheme"] as string;
    public bool IsHttps => Scheme == "https";
    public string Protocol => _env["owin.RequestProtocol"] as string;
    public string ContentType => _env["owin.RequestHeaders"] is IDictionary<string, string[]> headers && headers.TryGetValue("Content-Type", out var values) ? values[0] : null;
    public Stream Body => _env["owin.RequestBody"] as Stream;
    public string Path => _env["owin.RequestPath"] as string;

    public IReadOnlyDictionary<string, StringValues> Query
    {
        get => _query ??= ParseQuery(_env["owin.RequestQueryString"] as string);
        set => _query = value;
    }

    public IReadOnlyDictionary<string, StringValues> Form
    {
        get => _formValue ?? _form;
        set => _formValue = value;
    }

    private static IReadOnlyDictionary<string, StringValues> ParseQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return EmptyDictionary<string, StringValues>.Instance;
        }

        var queryCollection = new Dictionary<string, StringValues>();
        var span = query.AsSpan();

        int index;

        while ((index = span.IndexOf('&')) != -1)
        {
            var pair = span.Slice(0, index);
            var pairIndex = pair.IndexOf('=');

            span = span.Slice(index + 1);

            if (pairIndex == -1)
            {
                queryCollection[pair.ToString()] = StringValues.Empty;
                continue;
            }

            var keySpan = pair.Slice(0, pairIndex);
            var mergeKey = false;

            if (keySpan.Length > 2 && keySpan[keySpan.Length - 2] == '[' && keySpan[keySpan.Length - 1] == ']')
            {
                mergeKey = true;
                keySpan = keySpan.Slice(0, keySpan.Length - 2);
            }

            var key = keySpan.ToString();
            var value = pair.Slice(pairIndex + 1).ToString();

            if (key.Contains('%')) key = Uri.UnescapeDataString(key);
            if (value.Contains('%')) value = Uri.UnescapeDataString(value);

            if (mergeKey && queryCollection.TryGetValue(key, out var values))
            {
                var array = new string[values.Count + 1];
                ((IList<string>)values).CopyTo(array, 0);
                array[array.Length - 1] = value;
                queryCollection[key] = new StringValues(array);
            }
            else
            {
                queryCollection[key] = new StringValues(value);
            }
        }

        return queryCollection;
    }
}
