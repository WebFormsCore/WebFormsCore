using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using WebFormsCore.UI;

namespace WebFormsCore;

public class CompressedViewStateManager : ViewStateManager
{
	public CompressedViewStateManager(IServiceProvider serviceProvider, IOptions<ViewStateOptions>? options = null)
		: base(serviceProvider, options)
	{
		Compression = options?.Value.DefaultCompression ?? Compression;
	}

#if NET
	public ViewStateCompression Compression { get; set; } = ViewStateCompression.Brotoli;
#else
    public ViewStateCompression Compression { get; set; } = ViewStateCompression.GZip;
#endif

	protected override bool TryDecompress(byte compressionByte, int length, Span<byte> data, [NotNullWhen(true)] out IMemoryOwner<byte>? newOwner, out int actualLength)
	{
		var compression = (ViewStateCompression) compressionByte;

		if (compression == ViewStateCompression.GZip)
		{
			var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
			var decoded = decodedOwner.Memory.Span;

			if (!TryDecompress(data, decoded, out actualLength))
			{
				throw new ViewStateException("Could not decompress the viewstate");
			}

			newOwner = decodedOwner;
			return true;
		}

#if NET
		if (compression == ViewStateCompression.Brotoli)
		{
			var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
			var decoded = decodedOwner.Memory.Span;

			if (!BrotliDecoder.TryDecompress(data, decoded, out actualLength))
			{
				throw new ViewStateException("Could not decompress the viewstate");
			}

			newOwner = decodedOwner;
			return true;
		}
#endif

		newOwner = null;
		actualLength = 0;
		return false;
	}

	private static unsafe bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int length)
	{
		fixed (byte* pBuffer = &source[0])
		{
			using var stream = new UnmanagedMemoryStream(pBuffer, source.Length);
			using var deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
			try
			{
				length = deflateStream.Read(destination);
				return true;
			}
			catch
			{
				length = 0;
				return false;
			}
		}
	}

	protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out byte compressionByte, out int length)
	{
#if NET
		if (Compression == ViewStateCompression.Brotoli && BrotliEncoder.TryCompress(source, destination, out length) && length <= source.Length)
		{
			compressionByte = (byte)ViewStateCompression.Brotoli;
			return true;
		}
#endif

		if (Compression == ViewStateCompression.GZip && TryCompressGzip(source, destination, out length) && length <= source.Length)
		{
			compressionByte = (byte)ViewStateCompression.GZip;
			return true;
		}

		length = 0;
		compressionByte = 0;
		return false;
	}

	private static unsafe bool TryCompressGzip(ReadOnlySpan<byte> source, Span<byte> destination, out int length)
	{
		fixed (byte* pBuffer = &destination[0])
		{
			using var destinationStream = new UnmanagedMemoryStream(pBuffer, destination.Length, destination.Length, FileAccess.Write);
			using var deflateStream = new DeflateStream(destinationStream, CompressionMode.Compress, true);
			try
			{
				deflateStream.Write(source);
				deflateStream.Close();
				length = (int)destinationStream.Position;
				return true;
			}
			catch
			{
				length = 0;
				return false;
			}
		}
	}
}
