using System.Text.Json.Serialization;

namespace WebFormsCore.UI.WebControls;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(ICollection<string>))]
[JsonSerializable(typeof(ListItemValues))]
internal partial class JsonContext : JsonSerializerContext
{
}
