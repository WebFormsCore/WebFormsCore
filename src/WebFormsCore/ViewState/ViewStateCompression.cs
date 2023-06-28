namespace WebFormsCore;

public enum ViewStateCompression : byte
{
    /// <summary>
    /// The view state is not compressed.
    /// </summary>
    Raw = 0,

    /// <summary>
    /// The view state is compressed with Brotli.
    /// </summary>
    /// <remarks>
    /// Only supported in .NET 6+.
    /// </remarks>
    Brotoli = 1,

    /// <summary>
    /// The view state is compressed with GZip.
    /// </summary>
    GZip = 2
}