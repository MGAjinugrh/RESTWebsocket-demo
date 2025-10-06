using System.Text.Json.Serialization;

namespace Common.Models;

public class PresentBook
{
    [JsonPropertyName("id")] public uint Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = null!;
    [JsonPropertyName("author")] public string? Author { get; set; }
    [JsonPropertyName("datePublished")] public DateTime? DatePublished { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonIgnore] public BookStatus Status { get; set; }
    [JsonPropertyName("status")] public string StatusDisplay => Status.GetDisplayName();
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("createdBy")] public string CreatedBy { get; set; } = null!;
    [JsonPropertyName("modifiedAt")] public DateTime? ModifiedAt { get; set; }
    [JsonPropertyName("modifiedBy")] public string ModifiedBy { get; set; } = "-";
}
