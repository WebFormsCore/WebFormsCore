using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WebFormsCore.Security;

public enum CspMode
{
    Nonce,
    Sha256
}

public class CspDirective : ICollection<string>
{
    private readonly Csp _csp;
    private readonly HashSet<string> _sourceList;
    private CspMode? _mode;

    public CspDirective(Csp csp, string name, string? defaultValue = null)
    {
        _csp = csp;
        Name = name;
        _sourceList = new HashSet<string>();

        if (defaultValue != null)
        {
            _sourceList.Add(defaultValue);
        }
    }

    public CspMode Mode
    {
        get => _mode ?? _csp.DefaultMode;
        set => _mode = value;
    }

    public string Name { get; }

    public string GenerateNonce()
    {
        const int length = 9;
        var nonceBytes = ArrayPool<byte>.Shared.Rent(length);
#if NET
        RandomNumberGenerator.Fill(nonceBytes);
#else
        using var rng = new RNGCryptoServiceProvider();
        rng.GetBytes(nonceBytes);
#endif
        var result = Convert.ToBase64String(nonceBytes, 0, length);
        ArrayPool<byte>.Shared.Return(nonceBytes);
        _sourceList.Add($"'nonce-{result}'");
        return result;
    }

    public void AddInlineHash(string code)
    {
        _sourceList.Add(GetHash(code));
    }

    public void AddUnsafeInlineHash(string code)
    {
        _sourceList.Add("'unsafe-hashes'");
        _sourceList.Add(GetHash(code));
    }

    public static string GetHash(string code)
    {
#if NET
        var encoding = Encoding.UTF8;
        var byteLength = encoding.GetMaxByteCount(code.Length);
        if (byteLength > 4096)
        {
            byteLength = encoding.GetByteCount(code);
        }

        using var bytes = MemoryPool<byte>.Shared.Rent(byteLength);
        Span<byte> sha256Bytes = stackalloc byte[32];

        var length = Encoding.UTF8.GetBytes(code, bytes.Memory.Span);
        var span = bytes.Memory.Span.Slice(0, length);

        SHA256.TryHashData(span, sha256Bytes, out var bytesWritten);
        Debug.Assert(bytesWritten == sha256Bytes.Length);

        var base64 = Convert.ToBase64String(sha256Bytes);

            return $"'sha256-{base64}'";
#else
        var bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(code));

        try
        {
            var length = Encoding.UTF8.GetBytes(code, bytes);

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes, 0, length);
            var base64 = Convert.ToBase64String(hash);

            return $"'sha256-{base64}'";
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
#endif
    }

    public void Write(StringBuilder builder)
    {
        if (_sourceList.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("; ");
        }

        builder.Append(Name);

        foreach (var item in _sourceList)
        {
            builder.Append(' ');
            builder.Append(item);
        }
    }

    public HashSet<string>.Enumerator GetEnumerator() => _sourceList.GetEnumerator();

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_sourceList).GetEnumerator();

    public void Add(string item)
    {
        _sourceList.Add(item);
    }

    public void Clear()
    {
        _sourceList.Clear();
    }

    public bool Contains(string item)
    {
        return _sourceList.Contains(item);
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        _sourceList.CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        return _sourceList.Remove(item);
    }

    public int Count => _sourceList.Count;

    public bool IsReadOnly => false;
}
