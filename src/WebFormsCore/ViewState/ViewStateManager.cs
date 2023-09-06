using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.Options;
using WebFormsCore.Options;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

public class ViewStateManager : IViewStateManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ViewStateOptions>? _options;
    private readonly HashAlgorithm _hashAlgorithm;
    private readonly int _hashLength;

    public ViewStateManager(IServiceProvider serviceProvider, IOptions<ViewStateOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _hashAlgorithm = !string.IsNullOrEmpty(options?.Value.EncryptionKey)
            ? new HMACSHA256(Encoding.UTF8.GetBytes(options!.Value.EncryptionKey))
            : SHA256.Create();
        _hashLength = _hashAlgorithm.HashSize / 8;
    }

#if NET
    public ViewStateCompression Compression { get; set; } = ViewStateCompression.Brotoli;
#else
    public ViewStateCompression Compression { get; set; } = ViewStateCompression.GZip;
#endif

    /// <summary>
    /// Header length: compression + length + control count
    /// </summary>
    private const int HeaderLength = sizeof(byte) + sizeof(ushort) + sizeof(ushort);

    public bool EnableViewState => _options?.Value.Enabled ?? true;

    public IMemoryOwner<byte> WriteBase64(Control control, out int length)
    {
        var writer = new ViewStateWriter(_serviceProvider);

        try
        {
            ushort controlCount = 0;

            using var enumerator = new ViewStateControlEnumerator(control);

            while (enumerator.MoveNext())
            {
                enumerator.Current.WriteViewState(ref writer);
                controlCount++;
            }

            var state = writer.Span;
            var maxLength = Base64.GetMaxEncodedToUtf8Length(state.Length + HeaderLength + _hashLength);

            if (maxLength > (_options?.Value.MaxBytes ?? 102400))
            {
                throw new ViewStateException("Viewstate exceeds maximum size");
            }

            var array = ArrayPool<byte>.Shared.Rent(maxLength);
            var result = array.AsSpan();

            var header = result.Slice(0, HeaderLength);
            var hash = result.Slice(HeaderLength, _hashLength);
            var data = result.Slice(HeaderLength + _hashLength);

            // ReSharper disable once InlineOutVariableDeclaration
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
            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(3, 2), controlCount);

            ComputeHash(array, dataLength, hash);

            Base64.EncodeToUtf8InPlace(result, dataLength + HeaderLength + _hashLength, out length);

            return new ArrayMemoryOwner(array, length, true);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private void ComputeHash(byte[] data, int dataLength, Span<byte> hash)
    {
        var offset = HeaderLength + _hashLength;

#if NETSTANDARD2_0
        _hashAlgorithm.ComputeHash(data, offset, dataLength).CopyTo(hash);
#else
        _hashAlgorithm.TryComputeHash(data.AsSpan(offset, dataLength), hash, out _);
#endif
    }

    public async ValueTask<HtmlForm?> LoadFromRequestAsync(IHttpContext context, Page page)
    {
        if (!EnableViewState)
        {
            return null;
        }

        var request = context.Request;
        var isPostback = request.Method == "POST";

        if (!isPostback)
        {
            return null;
        }

        if (request.Form.TryGetValue("wfcPageState", out var pageState))
        {
            await LoadFromBase64Async(page, pageState.ToString());
        }

        if (!request.Form.TryGetValue("wfcForm", out var formId) ||
            !request.Form.TryGetValue("wfcFormState", out var formState))
        {
            return null;
        }

        var form = page.Forms.FirstOrDefault(i => i.UniqueID == formId);

        if (form != null && !string.IsNullOrEmpty(formState))
        {
            await LoadFromBase64Async(form, formState.ToString());
        }

        return form;
    }

    public async ValueTask LoadFromBase64Async(Control control, string viewState)
    {
        using var reader = CreateReader(viewState);

        await LoadViewStateAsync(control, reader);
    }

    public async ValueTask LoadFromArrayAsync(Control control, byte[] viewState)
    {
        using var reader = CreateReader(new ArrayMemoryOwner(viewState, viewState.Length, false));

        await LoadViewStateAsync(control, reader);
    }

    private ViewStateReaderOwner CreateReader(ArrayMemoryOwner arrayOwner)
    {
        var totalHeaderLength = HeaderLength + _hashLength;

        if (arrayOwner.Memory.Length < totalHeaderLength)
        {
            throw new ViewStateException("Viewstate is too short");
        }

        var span = arrayOwner.Memory.Span;

        var header = span.Slice(0, HeaderLength);
        var hash = span.Slice(HeaderLength, _hashLength);
        var data = span.Slice(HeaderLength + _hashLength);

        Span<byte> computedHash = stackalloc byte[_hashLength];
        ComputeHash(arrayOwner.Array, data.Length, computedHash);

        if (!computedHash.SequenceEqual(hash))
        {
            throw new ViewStateException("The viewstate hash is invalid");
        }

        IMemoryOwner<byte> owner = arrayOwner;
        var compression = (ViewStateCompression) header[0];
        var controlCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(3, 2));

        var offset = HeaderLength + _hashLength;

        int actualLength;
        var length = (int)BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1, 2));

        if (compression == ViewStateCompression.GZip)
        {
            var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
            var decoded = decodedOwner.Memory.Span;

            if (!TryDecompress(data, decoded, out actualLength))
            {
                throw new ViewStateException("Could not decompress the viewstate");
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

            if (!BrotliDecoder.TryDecompress(data, decoded, out actualLength))
            {
                throw new ViewStateException("Could not decompress the viewstate");
            }

            owner.Dispose();
            owner = decodedOwner;
            offset = 0;
        }
#endif
        else
        {
            actualLength = data.Length;
        }

        if (actualLength != length)
        {
            throw new ViewStateException("The viewstate length does not match the header");
        }

        return new ViewStateReaderOwner(owner.Memory, _serviceProvider, offset, controlCount, owner);
    }

    private ViewStateReaderOwner CreateReader(string base64)
    {
        var totalHeaderLength = HeaderLength + _hashLength;
        var encoding = Encoding.UTF8;
        var byteLength = encoding.GetByteCount(base64);

        if (byteLength > (_options?.Value.MaxBytes ?? 102400))
        {
            throw new ViewStateException("Viewstate exceeds maximum size");
        }

        if (byteLength < totalHeaderLength)
        {
            throw new ViewStateException("Viewstate is too short");
        }

        var array = ArrayPool<byte>.Shared.Rent(byteLength);
        var span = array.AsSpan();

        byteLength = encoding.GetBytes(base64, span);
        span = span.Slice(0, byteLength);

        var result = Base64.DecodeFromUtf8InPlace(span, out var base64Length);

        if (result != OperationStatus.Done)
        {
            throw new ViewStateException("Could not decode base64");
        }

        return CreateReader(new ArrayMemoryOwner(array, base64Length, true));
    }

    private async ValueTask LoadViewStateAsync(Control owner, ViewStateReaderOwner reader)
    {
        var enumerator = new ViewStateControlEnumerator(owner);
        var actualControlCount = 0;

        while (true)
        {
            var control = LoadViewState(ref enumerator, reader, ref actualControlCount);

            if (control == null) break;

            await control.AfterPostBackLoadAsync();
        }

        if (actualControlCount != reader.ControlCount)
        {
            throw new ViewStateException("The control count does not match the viewstate");
        }

        enumerator.Dispose();
    }

    /// <summary>
    /// Try to load the view state for as many controls as possible with the span-reader.
    /// </summary>
    private static IPostBackLoadHandler? LoadViewState(
        ref ViewStateControlEnumerator controls,
        ViewStateReaderOwner owner,
        ref int actualControlCount)
    {
        var reader = owner.CreateReader();

        try
        {
            while (controls.MoveNext())
            {
                var control = controls.Current!;

                control.LoadViewState(ref reader);
                actualControlCount++;

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

    private sealed class ArrayMemoryOwner : IMemoryOwner<byte>
    {
        private readonly bool _returnToPool;

        public ArrayMemoryOwner(byte[] array, int length, bool returnToPool)
        {
            Array = array;
            _returnToPool = returnToPool;
            Memory = new Memory<byte>(array, 0, length);
        }

        public byte[] Array { get; }

        public Memory<byte> Memory { get; }

        public void Dispose()
        {
            if (_returnToPool)
            {
                ArrayPool<byte>.Shared.Return(Array);
            }
        }
    }

    private struct ViewStateControlEnumerator : IDisposable
    {
        private readonly (Control Control, int Index)[] _array;
        private int _currentIndex = -1;

        public ViewStateControlEnumerator(Control root, int depth = 512)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _array = ArrayPool<(Control, int)>.Shared.Rent(depth);
            Push(root, -1);
        }

        public Control Current => _array[_currentIndex].Control;

        public bool MoveNext()
        {
            while (_currentIndex >= 0)
            {
                var (currentControl, index) = _array[_currentIndex--];

                index++;

                if (index >= currentControl.Controls.Count)
                {
                    continue;
                }

                Push(currentControl, index);

                var nextControl = currentControl.Controls[index];

                if (nextControl is HtmlForm)
                {
                    continue;
                }

                Push(nextControl, -1);

                if (nextControl.EnableViewState)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(Control control, int index)
        {
            _array[++_currentIndex] = (control, index);
        }

        public void Dispose()
        {
            ArrayPool<(Control, int)>.Shared.Return(_array);
        }
    }
}
