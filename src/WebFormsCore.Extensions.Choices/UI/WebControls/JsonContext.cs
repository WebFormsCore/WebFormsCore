using System.Text.Json.Serialization;

namespace WebFormsCore.UI.WebControls;

[JsonSerializable(typeof(ICollection<string>))]
internal partial class JsonContext : JsonSerializerContext
{
}
