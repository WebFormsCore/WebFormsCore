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
		Compression = options?.Value.DefaultCompression ?? ViewStateCompression.Brotoli;
		CompressionLevel = options?.Value.CompressionLevel ?? CompressionLevel.Fastest;
	}

	public CompressionLevel CompressionLevel { get; set; }

	public ViewStateCompression Compression { get; set; }

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

	internal static int GetBrotliQualityFromCompressionLevel(CompressionLevel compressionLevel) => compressionLevel switch
	{
		CompressionLevel.NoCompression => 0,
		CompressionLevel.Fastest => 1,
		CompressionLevel.Optimal => 4,
		CompressionLevel.SmallestSize => 1,
		_ => throw new ArgumentException("Invalid compression level", nameof(compressionLevel))
	};

	protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out byte compressionByte, out int length)
	{
		if (Compression == ViewStateCompression.Brotoli && BrotliEncoder.TryCompress(source, destination, out length, GetBrotliQualityFromCompressionLevel(CompressionLevel), 22) && length <= source.Length)
		{
			compressionByte = (byte)ViewStateCompression.Brotoli;
			return true;
		}

		if (Compression == ViewStateCompression.GZip && TryCompressGzip(source, destination, out length) && length <= source.Length)
		{
			compressionByte = (byte)ViewStateCompression.GZip;
			return true;
		}

		length = 0;
		compressionByte = 0;
		return false;
	}

	private unsafe bool TryCompressGzip(ReadOnlySpan<byte> source, Span<byte> destination, out int length)
	{
		fixed (byte* pBuffer = &destination[0])
		{
			using var destinationStream = new UnmanagedMemoryStream(pBuffer, destination.Length, destination.Length, FileAccess.Write);
			using var deflateStream = new DeflateStream(destinationStream, CompressionLevel, leaveOpen: true);
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
