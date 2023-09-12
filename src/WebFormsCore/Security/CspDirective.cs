using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WebFormsCore.Security;

public class CspDirective : ICollection<string>
{
    protected readonly Csp Csp;
    protected readonly HashSet<string> SourceList;

    public string Name { get; }

    public CspDirective(Csp csp, string name, string? defaultValue = null)
    {
        Csp = csp;
        Name = name;
        SourceList = new HashSet<string>();

        if (defaultValue != null)
        {
            SourceList.Add(defaultValue);
        }
    }

    public void Write(StringBuilder builder)
    {
        if (SourceList.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("; ");
        }

        builder.Append(Name);

        var defaultDirective = Csp.DefaultSrc;

        if (defaultDirective == this)
        {
            foreach (var item in SourceList)
            {
                builder.Append(' ');
                builder.Append(item);
            }

            return;
        }

        foreach (var item in defaultDirective.SourceList)
        {
            builder.Append(' ');
            builder.Append(item);
        }

        foreach (var item in SourceList)
        {
            if (defaultDirective.SourceList.Contains(item))
            {
                continue;
            }

            builder.Append(' ');
            builder.Append(item);
        }
    }

    public HashSet<string>.Enumerator GetEnumerator() => SourceList.GetEnumerator();

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)SourceList).GetEnumerator();

    public void Add(string item)
    {
        if (TryAddUri(item))
        {
            return;
        }

        SourceList.Add(item);
    }

    public bool TryAddUri(string? item)
    {
        if (item is null or { Length: 0 })
        {
            return false;
        }

        if (item.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            SourceList.Add("data:");
            return true;
        }

        if (item.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
#if NET
            !item.Contains('*') &&
#else
            !item.Contains("*") &&
#endif
            Uri.TryCreate(item, UriKind.Absolute, out var uri))
        {
            Add(uri);
            return true;
        }

        return false;
    }

    public void Add(Uri uri)
    {
        if (uri.Scheme == "data")
        {
            SourceList.Add("data:");
            return;
        }

        SourceList.Add($"{uri.Scheme}://{uri.Host}");
    }

    public void Clear()
    {
        SourceList.Clear();
    }

    public bool Contains(string item)
    {
        return SourceList.Contains(item);
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        SourceList.CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        return SourceList.Remove(item);
    }

    public int Count => SourceList.Count;

    public bool IsReadOnly => false;
}
