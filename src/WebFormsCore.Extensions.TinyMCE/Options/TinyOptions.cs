using System.Text.Json.Serialization;

namespace WebFormsCore.Options;

public class TinyOptions
{
    internal static readonly TinyOptions Default = new();

    /// <summary>
    /// <c>true</c> to enable the branding plugin, which adds a small "Powered by Tiny" link to the bottom right corner of the editor. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// While self-hosting TinyMCE it's allowed to disable the branding plugin.
    /// However, Tiny encourages you to keep the branding enabled: https://www.tiny.cloud/legal/attribution-requirements/
    /// </remarks>
    [JsonPropertyName("branding")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Branding { get; set; }

    /// <summary>
    /// <c>true</c> to enable the "Upgrade" button in the top right corner of the editor. Defaults to <c>false</c>.
    /// </summary>
    [JsonPropertyName("promotion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Promotion { get; set; }

    /// <summary>
    /// The toolbar to use for the editor.
    /// </summary>
    [JsonPropertyName("toolbar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Toolbar { get; set; }

    /// <summary>
    /// The height of the editor. Defaults to 400.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; set; }
}
