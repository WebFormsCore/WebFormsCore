using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WebFormsCore.Security;

public class CspDirectiveGenerated : CspDirective
{
    private CspMode? _mode;

    public CspDirectiveGenerated(Csp csp, string name, string? defaultValue = null)
        : base(csp, name, defaultValue)
    {
    }

    public CspMode Mode
    {
        get => _mode ?? Csp.DefaultMode;
        set => _mode = value;
    }

    public string GenerateNonce()
    {
        if (Name is "script-src" or "style-src")
        {
            // Added for backward compatible with browsers that do not support 'nonce'
            SourceList.Add("'unsafe-inline'");
        }

        const int length = 9;
        var nonceBytes = ArrayPool<byte>.Shared.Rent(length);
        RandomNumberGenerator.Fill(nonceBytes);

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
        if (code.IndexOf('\r') != -1)
        {
            // TODO: Reduce allocations
            code = code.ReplaceLineEndings("\n");
        }

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
    }
}
