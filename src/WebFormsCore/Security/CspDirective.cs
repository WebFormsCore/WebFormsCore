using System;
using System.Buffers;
using System.Buffers.Text;
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

public class CspDirective
{
    public CspDirective(string name, string? defaultValue = null)
    {
        Name = name;
        SourceList = new HashSet<string>();

        if (defaultValue != null)
        {
            SourceList.Add(defaultValue);
        }
    }

    public CspMode Mode { get; set; } = CspMode.Nonce;

    public string Name { get; }

    public HashSet<string> SourceList { get; set; }

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
        SourceList.Add($"'nonce-{result}'");
        return result;
    }

    public void AddInlineHash(string code)
    {
        SourceList.Add(GetHash(code));
    }

    public void AddUnsafeInlineHash(string code)
    {
        SourceList.Add("'unsafe-hashes'");
        SourceList.Add(GetHash(code));
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
        if (SourceList.Count == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("; ");
        }

        builder.Append(Name);

        foreach (var item in SourceList)
        {
            builder.Append(' ');
            builder.Append(item);
        }
    }
}
