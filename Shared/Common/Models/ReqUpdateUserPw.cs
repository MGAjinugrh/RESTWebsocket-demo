using System.Text.Json.Serialization;

namespace Common.Models
{
    public class ReqUpdateUserPw
    {
        [JsonPropertyName("password")]
        public string Password { get; set; } = null!;

        [JsonPropertyName("rePassword")]
        public string RePassword { get; set; } = null!;
    }
}
