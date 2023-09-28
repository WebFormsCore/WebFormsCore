using System.Text.Json.Serialization;
using WebFormsCore.Options;

namespace WebFormsCore;

[JsonSerializable(typeof(TinyOptions))]
internal partial class JsonContext : JsonSerializerContext
{
}
