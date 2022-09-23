using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls
{
    public class HtmlForm : HtmlContainerControl
    {
        private const byte FlagRaw = 0;
#if NET
        private const byte FlagBrotoliEncoded = 1;
#endif
        private const int HeaderLength = 3;

        public HtmlForm()
            : base("form")
        {
        }
        
        public bool Global { get; set; }

        protected override async ValueTask OnInitAsync(CancellationToken token)
        {
            await base.OnInitAsync(token);

            Page.Forms.Add(this);
        }

        protected virtual Control ViewStateOwner => Global ? Page : this;

        protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
        {
            await base.RenderAttributesAsync(writer);
            await writer.WriteAttributeAsync("data-wfc-form", Global ? "global" : "scope");
        }

        protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
        {
            await base.RenderChildrenAsync(writer, token);

            await writer.WriteAsync(@"<input type=""hidden"" name=""__FORM"" value=""");
            await writer.WriteAsync(UniqueID);
            await writer.WriteAsync(@"""/>");


            using var viewState = GenerateViewState(out var length);

            await writer.WriteAsync(@"<input type=""hidden"" name=""__VIEWSTATE"" value=""");
            await writer.WriteAsync(viewState.Memory.Slice(0, length), token);
            await writer.WriteAsync(@"""/>");
        }

        public void LoadViewState(string base64)
        {
            var encoding = Encoding.UTF8;
            var byteLength = encoding.GetByteCount(base64);
            var owner = MemoryPool<byte>.Shared.Rent(byteLength);

            try
            {
                var span = owner.Memory.Span;

                byteLength = encoding.GetBytes(base64, span);
                span = span.Slice(0, byteLength);

                if (Base64.DecodeFromUtf8InPlace(span, out var base64Length) != OperationStatus.Done)
                {
                    throw new InvalidOperationException("Could not decode base64");
                } 

                span = span.Slice(0, base64Length);

                var header = span.Slice(0, HeaderLength);
                var data = span.Slice(HeaderLength);

                var flag = header[0];
                var length = (int)BinaryPrimitives.ReadUInt16BigEndian(header.Slice(1, 2));

#if NET
                if (flag == FlagBrotoliEncoded)
                {
                    var decodedOwner = MemoryPool<byte>.Shared.Rent(length);
                    var decoded = decodedOwner.Memory.Span;

                    if (!BrotliDecoder.TryDecompress(data, decoded, out length))
                    {
                        throw new InvalidOperationException("Could not decompress the viewstate");
                    }

                    owner.Dispose();
                    owner = decodedOwner;
                    data = decoded.Slice(0, length);
                }
#endif

                var reader = new ViewStateReader(data, ServiceProvider);
                Global = reader.Read<bool>();
                ViewStateOwner.ReadViewState(ref reader, this);
            }
            finally
            {
                owner.Dispose();
            }
        }

        public IMemoryOwner<byte> GenerateViewState(out int written)
        {
            var writer = new ViewStateWriter(ServiceProvider);

            try
            {
                writer.Write(Global);
                ViewStateOwner.WriteViewState(ref writer, this);
                
                var state = writer.Span;

                var maxLength = Base64.GetMaxEncodedToUtf8Length(state.Length + HeaderLength);
                var resultOwner = MemoryPool<byte>.Shared.Rent(maxLength);
                var result = resultOwner.Memory.Span;

                var header = result.Slice(0, HeaderLength);
                var data = result.Slice(HeaderLength);

                int length;
#if NET
                if (BrotliEncoder.TryCompress(state, data, out length) && length <= state.Length)
                {
                    header[0] = FlagBrotoliEncoded;
                }
                else
#endif
                {
                    header[0] = FlagRaw;
                    state.CopyTo(data);
                    length = state.Length;
                }

                BinaryPrimitives.WriteUInt16BigEndian(header.Slice(1, 2), (ushort)state.Length);

                Base64.EncodeToUtf8InPlace(result, length + HeaderLength, out written);

                return resultOwner;
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    public class PlaceHolder : Control
    {
    }
}
