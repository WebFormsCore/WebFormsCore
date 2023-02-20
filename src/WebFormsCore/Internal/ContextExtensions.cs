using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace WebFormsCore;

public static class ContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMethod(this HttpRequest request, string method)
    {
#if NETFRAMEWORK
        return request.HttpMethod.Equals(method, StringComparison.OrdinalIgnoreCase);
#else
        return request.Method.Equals(method, StringComparison.OrdinalIgnoreCase);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetFormValue(this HttpRequest request, string? name)
    {
        if (name is null) return null;

#if NETFRAMEWORK
        return request.Form[name];
#else
        return request.Form[name] is { Count: > 0 } values ? values[0] : null;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream GetOutputStream(this HttpResponse response)
    {
#if NETFRAMEWORK
        return response.OutputStream;
#else
        return response.Body;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stream GetInputStream(this HttpRequest request)
    {
#if NETFRAMEWORK
        return request.InputStream;
#else
        return request.Body;
#endif
    }
}
