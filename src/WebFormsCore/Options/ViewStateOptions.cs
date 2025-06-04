using System.IO.Compression;

namespace WebFormsCore;

public class ViewStateOptions
{
    /// <summary>
    /// Whether the view state is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to compact the view state. When compacting, the key is not included in the view state.
    /// It is recommended to disable this option during development to prevent view state corruption when adding new properties.
    /// </summary>
    public bool Compact { get; set; } = true;

    /// <summary>
    /// The encryption key to hash the view state with.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// The maximum number of bytes that the view state can be.
    /// </summary>
    public int MaxBytes { get; set; } = 102400;

    /// <summary>
    /// The maximum number of items that a collection (e.g. list, array) can contain.
    /// </summary>
    public int MaxCollectionLength { get; set; } = 1024;

    /// <summary>
    /// The maximum number of characters that a string can contain.
    /// </summary>
    public int MaxStringLength { get; set; } = 32768;

    /// <summary>
    /// The default compression algorithm to use.
    /// </summary>
    public ViewStateCompression DefaultCompression { get; set; } = ViewStateCompression.Brotoli;

    /// <summary>
    /// The default compression level to use.
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;
}
