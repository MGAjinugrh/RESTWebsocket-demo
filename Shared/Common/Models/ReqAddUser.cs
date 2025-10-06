using System.Text.Json.Serialization;

namespace Common.Models
{
    public class ReqAddUser{
        [JsonPropertyName("roleid")]
        public int RoleId { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; } = null!;

        [JsonPropertyName("password")]
        public string Password { get; set; } = null!;

        [JsonPropertyName("rePassword")]
        public string RePassword { get; set; } = null!;
    }
}
