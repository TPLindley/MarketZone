using System.Text.Json.Serialization;

namespace mzConfigure.Models;

public class Special
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";
}
