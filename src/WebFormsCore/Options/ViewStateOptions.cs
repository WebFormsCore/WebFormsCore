using System.IO.Compression;

namespace WebFormsCore;

public class ViewStateOptions
{
    public bool Enabled { get; set; } = true;

    public bool Compact { get; set; } = true;

    public string? EncryptionKey { get; set; }

    public int MaxBytes { get; set; } = 102400;

    public ViewStateCompression? DefaultCompression { get; set; }

    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;
}
