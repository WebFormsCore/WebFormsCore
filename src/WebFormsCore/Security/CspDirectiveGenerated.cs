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
        var encoding = Encoding.UTF8;
        var byteLength = encoding.GetMaxByteCount(code.Length);
        if (byteLength > 4096)
        {
            byteLength = encoding.GetByteCount(code);
        }

#if NET
        using var bytes = MemoryPool<byte>.Shared.Rent(byteLength);
        Span<byte> sha256Bytes = stackalloc byte[32];

        var length = Encoding.UTF8.GetBytes(code, bytes.Memory.Span);
        var span = bytes.Memory.Span.Slice(0, length);

        SHA256.TryHashData(span, sha256Bytes, out var bytesWritten);
        Debug.Assert(bytesWritten == sha256Bytes.Length);

        var base64 = Convert.ToBase64String(sha256Bytes);

        return $"'sha256-{base64}'";
#else
        var bytes = ArrayPool<byte>.Shared.Rent(byteLength);

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
}
