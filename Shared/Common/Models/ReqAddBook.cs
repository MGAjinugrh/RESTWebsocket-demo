using System.Text.Json.Serialization;

namespace Common.Models;

public class ReqAddBook
{
    [JsonPropertyName("title")] public string Title { get; set; } = null!;
    [JsonPropertyName("author")] public string Author { get; set; } = "-";
    [JsonPropertyName("datePublished")] public DateTime? DatePublished { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
}
