using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WebFormsCore.Options;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

public class ViewStateManager : IViewStateManager
{
#if NETFRAMEWORK
    private static readonly Action<HttpRequest, NameValueCollection> SetRequestForm;
    private static readonly Type FormType;
    private static readonly ConstructorInfo? FormConstructor;

    static ViewStateManager()
    {
        var field = typeof(HttpRequest).GetField("_form", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Could not find _form field on HttpRequest");

        FormType = field.FieldType;
        FormConstructor = FormType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Array.Empty<Type>(), null);

        var parameter = Expression.Parameter(typeof(HttpRequest), "request");
        var parameter2 = Expression.Parameter(typeof(NameValueCollection), "form");
        Expression form = parameter2;

        if (FormType != typeof(NameValueCollection))
        {
            form = Expression.Convert(parameter2, FormType);
        }

        var body = Expression.Assign(Expression.Field(parameter, field), form);
        SetRequestForm = Expression.Lambda<Action<HttpRequest, NameValueCollection>>(body, parameter, parameter2).Compile();
    }
#endif

    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ViewStateOptions> _options;
    private readonly HashAlgorithm _hashAlgorithm;
    private readonly int _hashLength;

    public ViewStateManager(IServiceProvider serviceProvider, IOptions<ViewStateOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _hashAlgorithm = !string.IsNullOrEmpty(options.Value.EncryptionKey)
            ? new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.EncryptionKey))
            : SHA256.Create();
        _hashLength = _hashAlgorithm.HashSize / 8;
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

    private static IEnumerable<Control> GetControls(Control owner)
    {
        return owner.EnumerateControls(static c => c is not HtmlForm);
    }

    public bool EnableViewState => _options.Value.Enabled;

    public IMemoryOwner<byte> Write(Control control, out int length)
    {
        var writer = new ViewStateWriter(_serviceProvider);

        try
        {
            foreach (var child in GetControls(control))
            {
                child.WriteViewState(ref writer);
            }

            var state = writer.Span;
            var maxLength = Base64.GetMaxEncodedToUtf8Length(state.Length + HeaderLength + _hashLength);
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

            ComputeHash(array, dataLength, hash);

            Base64.EncodeToUtf8InPlace(result, dataLength + HeaderLength + _hashLength, out length);

            return new ArrayMemoryOwner<byte>(array, length);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private void ComputeHash(byte[] data, int dataLength, Span<byte> hash)
    {
        var offset = HeaderLength + _hashLength;

#if NETFRAMEWORK
        _hashAlgorithm.ComputeHash(data, offset, dataLength).CopyTo(hash);
#else
        _hashAlgorithm.TryComputeHash(data.AsSpan(offset, dataLength), hash, out _);
#endif
    }

    public async ValueTask<HtmlForm?> LoadAsync(HttpContext context, Page page)
    {
        if (!EnableViewState)
        {
            return null;
        }

        var request = context.Request;
        var isPostback = request.IsMethod("POST");

        if (!isPostback)
        {
            return null;
        }

        if (context.Request.ContentType == "application/json")
        {
            var json = await JsonSerializer.DeserializeAsync<JsonDocument>(request.GetInputStream());

#if NETFRAMEWORK
            var data = (NameValueCollection) (FormConstructor?.Invoke(Array.Empty<object>()) ?? Activator.CreateInstance(FormType) ?? throw new InvalidOperationException("Could not create form"));

            if (json != null)
            {
                foreach (var property in json.RootElement.EnumerateObject())
                {
                    data.Add(property.Name, property.Value.GetString());
                }
            }

            SetRequestForm(request, data);
#else
            if (json != null)
            {
                var fields = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();

                foreach (var property in json.RootElement.EnumerateObject())
                {
                    fields[property.Name] = property.Value.GetString();
                }

                request.Form = new FormCollection(fields);
            }
#endif
        }

        page.IsPostBack = true;

        var pageState = request.GetFormValue("__PAGESTATE");

        if (pageState != null)
        {
            await LoadViewStateAsync(page, pageState);
        }

        var formId = request.GetFormValue("__FORM");

        if (formId == null)
        {
            return null;
        }

        var formState = request.GetFormValue("__FORMSTATE");
        var form = page.Forms.FirstOrDefault(i => i.UniqueID == formId);

        if (form != null && formState != null)
        {
            await LoadViewStateAsync(form, formState);
        }

        return form;

    }

    private ViewStateReaderOwner CreateReader(string base64)
    {
        var encoding = Encoding.UTF8;
        var byteLength = encoding.GetByteCount(base64);
        var array = ArrayPool<byte>.Shared.Rent(byteLength);
        IMemoryOwner<byte> owner = new ArrayMemoryOwner<byte>(array, byteLength);
        var span = array.AsSpan();

        byteLength = encoding.GetBytes(base64, span);
        span = span.Slice(0, byteLength);

        if (Base64.DecodeFromUtf8InPlace(span, out var base64Length) != OperationStatus.Done)
        {
            throw new InvalidOperationException("Could not decode base64");
        }

        span = span.Slice(0, base64Length);

        var header = span.Slice(0, HeaderLength);
        var hash = span.Slice(HeaderLength, _hashLength);
        var data = span.Slice(HeaderLength + _hashLength);

        Span<byte> computedHash = stackalloc byte[_hashLength];
        ComputeHash(array, data.Length, computedHash);

        if (!computedHash.SequenceEqual(hash))
        {
            throw new InvalidOperationException("The viewstate hash is invalid");
        }

        var compression = (ViewStateCompression) header[0];
        var length = (int)BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1, 2));
        var offset = HeaderLength + _hashLength;

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

    private async ValueTask LoadViewStateAsync(Control owner, string viewState)
    {
        using var wrapper = CreateReader(viewState);
        using var enumerator = GetControls(owner).GetEnumerator();

        while (true)
        {
            var control = LoadViewState(enumerator, wrapper);

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

    private sealed class ArrayMemoryOwner<T> : IMemoryOwner<byte>
    {
        private readonly byte[] _array;

        public ArrayMemoryOwner(byte[] array, int length)
        {
            _array = array;
            Memory = new Memory<byte>(array, 0, length);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_array);
        }

        public Memory<byte> Memory { get; }
    }
}
