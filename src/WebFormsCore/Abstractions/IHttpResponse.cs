using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore;

public interface IHttpResponse
{
    Stream Body { get; }

    string? ContentType { get; set; }

    IDictionary<string, StringValues> Headers { get; }
}
