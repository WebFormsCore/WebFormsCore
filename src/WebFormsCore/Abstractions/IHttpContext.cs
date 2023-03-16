using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore;

public interface IHttpContext
{
    IHttpRequest Request { get; }

    IHttpResponse Response { get; }

    IServiceProvider RequestServices { get; }

    CancellationToken RequestAborted { get; }
}

public interface IHttpResponse
{
    Stream Body { get; }

    string ContentType { get; set; }

    IDictionary<string, StringValues> Headers { get; }
}

public interface IHttpRequest
{
    string Method { get; }

    string? ContentType { get; }

    Stream Body { get; }

    string Path { get; }

    IReadOnlyDictionary<string, StringValues> Form { get; set; }
}
