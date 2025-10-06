using System.Text.Json.Serialization;

namespace Common.Models
{
    public class PresentUser
    {
        [JsonPropertyName("id")] public uint Id { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; } = null!;
        [JsonPropertyName("username")] public string Username { get; set; } = null!;
        [JsonPropertyName("isActive")] public bool IsActive { get; set; }
        [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }
        [JsonPropertyName("CreatedAt")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("CreatedBy")] public string CreatedBy { get; set; } = "-";
        [JsonPropertyName("modifiedAt")] public DateTime? ModifiedAt { get; set; }
        [JsonPropertyName("modifiedBy")] public string ModifiedBy { get; set; } = "-";

    }
}
