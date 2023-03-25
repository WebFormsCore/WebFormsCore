using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore;

public interface IHttpResponse
{
    Stream Body { get; }

    string? ContentType { get; set; }

    IDictionary<string, StringValues> Headers { get; }
}

public static class HttpResponseExtensions
{
    public static async Task WriteAsync(this IHttpResponse response, string content, CancellationToken token = default)
    {
        var encoding = Encoding.UTF8;
        var length = encoding.GetMaxByteCount(content.Length);
        var array = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var bytes = encoding.GetBytes(content, array);

            await response.Body.WriteAsync(array, 0, bytes, token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
