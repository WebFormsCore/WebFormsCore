using System;
using System.Buffers;
using System.Collections.Generic;
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
        SourceList = new List<string>();

        if (defaultValue != null)
        {
            SourceList.Add(defaultValue);
        }
    }

    public CspMode Mode { get; set; } = CspMode.Nonce;

    public string Name { get; }

    public List<string> SourceList { get; set; }

    public string GenerateNonce()
    {
        var nonceBytes = ArrayPool<byte>.Shared.Rent(16);
#if NET
        RandomNumberGenerator.Fill(nonceBytes);
#else
        using var rng = new RNGCryptoServiceProvider();
        rng.GetBytes(nonceBytes);
#endif
        var result = Convert.ToBase64String(nonceBytes);
        ArrayPool<byte>.Shared.Return(nonceBytes);
        SourceList.Add($"'nonce-{result}'");
        return result;
    }

    public void AddInlineHash(string code)
    {
        var source = CreateHash(code);

        SourceList.Add(source);
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

    public static string CreateHash(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        var base64 = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"'sha256-{base64}'";
    }
}
