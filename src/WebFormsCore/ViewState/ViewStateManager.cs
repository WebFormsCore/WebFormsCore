using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

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
            ? new HMACSHA256(Encoding.UTF8.GetBytes(options!.Value.EncryptionKey!))
            : SHA256.Create();
        _hashLength = _hashAlgorithm.HashSize / 8;
    }

    /// <summary>
    /// Header length: compression + length + control count
    /// </summary>
    private const int HeaderLength = sizeof(byte) + sizeof(ushort) + sizeof(ushort);

    public bool EnableViewState => _options?.Value.Enabled ?? true;

    public ValueTask<IMemoryOwner<byte>> WriteAsync(Control control, out int length)
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
            if (TryCompress(state, data, out var compressionByte, out var dataLength))
            {
                header[0] = compressionByte;
            }
            else
            {
                header[0] = 0;
                state.CopyTo(data);
                dataLength = state.Length;
            }

            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(1, 2), (ushort)state.Length);
            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(3, 2), controlCount);

            ComputeHash(array, dataLength, hash);

            Base64.EncodeToUtf8InPlace(result, dataLength + HeaderLength + _hashLength, out length);

            var owner = new ArrayMemoryOwner(array, length, true);

            return new ValueTask<IMemoryOwner<byte>>(owner);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private void ComputeHash(byte[] data, int dataLength, Span<byte> hash)
    {
        var offset = HeaderLength + _hashLength;

#if NETSTANDARD2_0 || NETFRAMEWORK
        var result = _hashAlgorithm.ComputeHash(data, offset, dataLength);

        CopyToHash(ref hash, result);
#else
        var success = _hashAlgorithm.TryComputeHash(data.AsSpan(offset, dataLength), hash, out var bytesWritten);

        if (success)
        {
            if (bytesWritten != hash.Length)
            {
                hash.Slice(bytesWritten).Fill(0);
            }
        }
        else
        {
            var result = _hashAlgorithm.ComputeHash(data, offset, dataLength);

            CopyToHash(ref hash, result);
        }
#endif
    }

    private static void CopyToHash(ref Span<byte> hash, byte[] result)
    {
        if (hash.Length == result.Length)
        {
            result.CopyTo(hash);
        }
        else if (hash.Length > result.Length)
        {
            result.CopyTo(hash);
            hash.Slice(result.Length).Fill(0);
        }
        else
        {
            result.AsSpan(0, hash.Length).CopyTo(hash);
        }
    }

    public async ValueTask LoadAsync(Control control, string viewState)
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
        var controlCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(3, 2));

        var offset = HeaderLength + _hashLength;

        int actualLength;
        var length = (int)BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1, 2));

        if (TryDecompress(header[0], length, data, out var decodedOwner, out actualLength))
        {
            owner.Dispose();
            owner = decodedOwner;
            offset = 0;
        }
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

    protected virtual bool TryDecompress(byte compressionByte, int length, Span<byte> data, [NotNullWhen(true)] out IMemoryOwner<byte>? newOwner, out int actualLength)
    {
        newOwner = null;
        actualLength = 0;
        return false;
    }

    protected virtual bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out byte compressionByte, out int length)
    {
        compressionByte = 0;
        length = 0;
        return false;
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
    private IPostBackLoadHandler? LoadViewState(
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
            Push(root, -2);
        }

        public Control Current => _array[_currentIndex].Control;

        public bool MoveNext()
        {
            while (_currentIndex >= 0)
            {
                var (currentControl, index) = _array[_currentIndex--];

                index++;

                if (index == 0 && !currentControl.ProcessChildrenInternal)
                {
                    continue;
                }

                if (index >= (currentControl.HasControls() ? currentControl.Controls.Count : 0))
                {
                    continue;
                }

                Push(currentControl, index);

                if (index < 0)
                {
                    return true;
                }

                var nextControl = currentControl.Controls[index];

                if (nextControl is HtmlForm or StreamPanel)
                {
                    continue;
                }

                Push(nextControl, -1);

                if (nextControl is { EnableViewState: true, ProcessControlInternal: true })
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
            ArrayPool<(Control, int)>.Shared.Return(_array, true);
        }
    }
}
