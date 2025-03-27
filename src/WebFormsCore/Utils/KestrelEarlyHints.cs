#if !WASM
#nullable disable
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace WebFormsCore;

internal static class KestrelEarlyHints
{
    public static ValueTask Send103EarlyHints(this HttpContext httpContext, string link)
    {
        return httpContext.Request.Protocol switch
        {
            "HTTP/1.0" or "HTTP/1.1" => Http1Writer.Send103EarlyHints(httpContext, link),
            "HTTP/2" => Http2Writer.Send103EarlyHints(httpContext, link),
            _ => default
        };
    }

    private static async ValueTask FromValueTaskT<T>(ValueTask<T> task) => await task;

    private static class Http1Writer
    {
        private delegate ValueTask<FlushResult> WriteDataToPipeAsync(
            ReadOnlySpan<byte> buffer,
            CancellationToken cancellationToken = default
        );

        private static readonly Type Http1Connection = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection, Microsoft.AspNetCore.Server.Kestrel.Core");
        private static readonly FieldInfo OutputProducerProperty = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetField("_http1Output", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo WriteDataToPipeMethod = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1OutputProducer, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetMethod("WriteDataToPipeAsync", BindingFlags.Public | BindingFlags.Instance);
        private static bool IsSupported = Http1Connection is not null && OutputProducerProperty is not null && WriteDataToPipeMethod is not null;

        private static ValueTask WriteAsync(HttpContext httpContext, ReadOnlySpan<byte> buffer)
        {
            if (!IsSupported)
            {
                Debug.Fail("Internals of Kestrel have changed");
                return default;
            }

            var feature = httpContext.Features.Get<IHttpConnectionFeature>();

            if (!Http1Connection.IsInstanceOfType(feature))
            {
                return default;
            }

            var output = OutputProducerProperty.GetValue(feature);
            var @delegate = WriteDataToPipeMethod.CreateDelegate<WriteDataToPipeAsync>(output);
            var task = @delegate(buffer);

            return task.IsCompletedSuccessfully ? default : FromValueTaskT(task);
        }

        public static async ValueTask Send103EarlyHints(HttpContext httpContext, string link)
        {
            var sb = new StringBuilder();
            sb.Append(httpContext.Request.Protocol == "HTTP/1.0" ? "HTTP/1.0" : "HTTP/1.1");
            sb.Append(" 103 Early Hint\r\n");
            sb.Append("Link: ");
            sb.Append(link);
            sb.Append("\r\n\r\n");

            var encoding = Encoding.UTF8;
            var array = ArrayPool<byte>.Shared.Rent(encoding.GetMaxByteCount(sb.Length));

            try
            {
                var count = encoding.GetBytes(sb.ToString(), array);
                var buffer = new ReadOnlySpan<byte>(array, 0, count);

                await WriteAsync(httpContext, buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

    private static class Http2Writer
    {
        private static readonly object UploadResumptionSupportedHttpStatusCode = 103;
        private static readonly Type Http2HeadersFrameFlags = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags, Microsoft.AspNetCore.Server.Kestrel.Core");
        private static readonly object EndHeadersFlag = Http2HeadersFrameFlags is null ? null : Enum.Parse(Http2HeadersFrameFlags, "END_HEADERS");
        private static readonly Type Http2Stream = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream, Microsoft.AspNetCore.Server.Kestrel.Core");
        private static readonly PropertyInfo Http2OutputProducerProperty = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetProperty("Output", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo WriteResponseHeadersMethod = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetMethod("WriteResponseHeaders", BindingFlags.Public | BindingFlags.Instance);
        private static readonly FieldInfo FrameWriterProperty = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2OutputProducer, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetField("_frameWriter", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo StreamIdProperty = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2OutputProducer, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetProperty("StreamId", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly ConstructorInfo HttpResponseHeadersConstructor = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetConstructor([typeof(Func<string, Encoding>)]);
        private static readonly object[] HttpResponseHeadersConstructorArguments = [null];
        private static readonly bool IsSupported = Http2Stream is not null && Http2OutputProducerProperty is not null && WriteResponseHeadersMethod is not null && FrameWriterProperty is not null && StreamIdProperty is not null && HttpResponseHeadersConstructor is not null;

        private static readonly MethodInfo FlushAsync = Type.GetType("Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter, Microsoft.AspNetCore.Server.Kestrel.Core")?.GetMethod("TimeFlushUnsynchronizedAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, [], null);

        private delegate ValueTask<FlushResult> WriteDataToPipeAsync();

        public static ValueTask Send103EarlyHints(HttpContext httpContext, string link)
        {
            if (!IsSupported)
            {
                Debug.Fail("Internals of Kestrel have changed");
                return default;
            }

            var feature = httpContext.Features.Get<IHttpConnectionFeature>();

            if (!Http2Stream.IsInstanceOfType(feature))
            {
                return default;
            }

            var output = Http2OutputProducerProperty.GetValue(feature);
            var frameWriter = FrameWriterProperty.GetValue(output);
            var streamId = StreamIdProperty.GetValue(output);
            var headers = (IHeaderDictionary) HttpResponseHeadersConstructor.Invoke(HttpResponseHeadersConstructorArguments);

            headers.Append("Link", link);

            WriteResponseHeadersMethod.Invoke(frameWriter, [streamId, UploadResumptionSupportedHttpStatusCode, EndHeadersFlag, headers]);

            var @delegate = FlushAsync.CreateDelegate<WriteDataToPipeAsync>(frameWriter);
            var task = @delegate();

            return task.IsCompletedSuccessfully ? default : FromValueTaskT(task);
        }

    }
}
#endif