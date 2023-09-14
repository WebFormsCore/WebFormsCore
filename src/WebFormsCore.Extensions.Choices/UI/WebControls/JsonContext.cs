using System.Text.Json.Serialization;

namespace WebFormsCore.UI.WebControls;

[JsonSerializable(typeof(ICollection<string>))]
[JsonSerializable(typeof(IEnumerable<string>))]
internal partial class JsonContext : JsonSerializerContext
{
}
