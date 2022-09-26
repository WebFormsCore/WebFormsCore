using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public class ViewStateManager : IViewStateManager
{
    private readonly IServiceProvider _serviceProvider;

    public ViewStateManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

#if NET
    public ViewStateCompression Compression { get; set; } = ViewStateCompression.Brotoli;
#else
    public ViewStateCompression Compression { get; set; } = ViewStateCompression.GZip;
#endif

    /// <summary>
    /// Header length: compression + length
    /// </summary>
    private const int HeaderLength = sizeof(byte) + sizeof(ushort);

    public IMemoryOwner<byte> Write(HtmlForm form, out int length)
    {
        var owner = form.ViewStateOwner;
        var writer = new ViewStateWriter(_serviceProvider);

        try
        {
            foreach (var child in owner.EnumerateControls())
            {
                child.WriteViewState(ref writer);
            }

            var state = writer.Span;

            var maxLength = Base64.GetMaxEncodedToUtf8Length(state.Length + HeaderLength);
            var resultOwner = MemoryPool<byte>.Shared.Rent(maxLength);
            var result = resultOwner.Memory.Span;

            var header = result.Slice(0, HeaderLength);
            var data = result.Slice(HeaderLength);

            int dataLength;
#if NET
            if (Compression == ViewStateCompression.Brotoli && BrotliEncoder.TryCompress(state, data, out dataLength) && dataLength <= state.Length)
            {
                header[0] = (byte)ViewStateCompression.Brotoli;
            }
            else
#endif
            if (Compression == ViewStateCompression.GZip && TryCompress(state, data, out dataLength) && dataLength <= state.Length)
            {
                header[0] = (byte)ViewStateCompression.GZip;
            }
            else
            {
                header[0] = (byte)ViewStateCompression.Raw;
                state.CopyTo(data);
                dataLength = state.Length;
            }

            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(1, 2), (ushort)state.Length);

            Base64.EncodeToUtf8InPlace(result, dataLength + HeaderLength, out length);

            return resultOwner;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public async ValueTask<HtmlForm?> LoadAsync(HttpContext context, Page page)
    {
        var request = context.Request;
        var method = request.HttpMethod;
        var isPostback = method == "POST";

        if (!isPostback)
        {
            return null;
        }

        page.IsPostBack = true;

        var formId = request.Form["__FORM"];
        var viewStateValue = request.Form["__VIEWSTATE"];
        var form = page.Forms.FirstOrDefault(i => i.UniqueID == formId);

        if (form == null || viewStateValue == null)
        {
            return null;
        }

        var owner = form.ViewStateOwner;
        using var wrapper = CreateReader(viewStateValue);
        using var enumerator = owner.EnumerateControls().GetEnumerator();

        await LoadViewStateAsync(enumerator, wrapper);

        return form;
    }

    internal ViewStateReaderOwner CreateReader(string base64)
    {
        var encoding = Encoding.UTF8;
        var byteLength = encoding.GetByteCount(base64);
        var owner = MemoryPool<byte>.Shared.Rent(byteLength);

        var span = owner.Memory.Span;

        byteLength = encoding.GetBytes(base64, span);
        span = span.Slice(0, byteLength);

        if (Base64.DecodeFromUtf8InPlace(span, out var base64Length) != OperationStatus.Done)
        {
            throw new InvalidOperationException("Could not decode base64");
        }

        span = span.Slice(0, base64Length);

        var header = span.Slice(0, HeaderLength);
        var offset = HeaderLength;
        var length = (int)BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1, 2));
        var data = span.Slice(HeaderLength);

        var compression = (ViewStateCompression) header[0];

        if (compression == ViewStateCompression.GZip)
        {
            var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
            var decoded = decodedOwner.Memory.Span;

            if (!TryDecompress(data, decoded, out length))
            {
                throw new InvalidOperationException("Could not decompress the viewstate");
            }

            owner.Dispose();
            owner = decodedOwner;
            offset = 0;
        }
#if NET
        else if (compression == ViewStateCompression.Brotoli)
        {
            var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
            var decoded = decodedOwner.Memory.Span;

            if (!BrotliDecoder.TryDecompress(data, decoded, out length))
            {
                throw new InvalidOperationException("Could not decompress the viewstate");
            }

            owner.Dispose();
            owner = decodedOwner;
            offset = 0;
        }
#endif

        return new ViewStateReaderOwner(owner, _serviceProvider, offset);
    }

    private static async ValueTask LoadViewStateAsync(IEnumerator<Control> controls, ViewStateReaderOwner owner)
    {
        while (true)
        {
            var control = LoadViewState(controls, owner);

            if (control == null) break;

            await control.AfterPostBackLoadAsync();
        }
    }

    /// <summary>
    /// Try to load the view state for as many controls as possible with the span-reader.
    /// </summary>
    private static IPostBackLoadHandler? LoadViewState(IEnumerator<Control> controls, ViewStateReaderOwner owner)
    {
        var reader = owner.CreateReader();

        try
        {
            while (controls.MoveNext())
            {
                var control = controls.Current!;

                control.LoadViewState(ref reader);

                if (control is IPostBackLoadHandler handler)
                {
                    return handler;
                }
            }

            return null;
        }
        finally
        {
            reader.Dispose();
        }
    }

    private static unsafe bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int length)
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
}
