using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore;

public interface IHttpRequest
{
    string Method { get; }

    string Scheme { get; }

    bool IsHttps { get; }

    string Protocol { get; }

    string? ContentType { get; }

    Stream Body { get; }

    string Path { get; }

    IReadOnlyDictionary<string, StringValues> Query { get; set; }

    IReadOnlyDictionary<string, StringValues> Form { get; set; }
}
