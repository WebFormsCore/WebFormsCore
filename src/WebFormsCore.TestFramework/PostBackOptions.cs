using System.Text.Json.Serialization;

namespace WebFormsCore;

public class PostBackOptions
{
    [JsonPropertyName("validate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Validate { get; set; }
}
