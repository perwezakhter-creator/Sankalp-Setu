using System.Text.Json.Serialization;

namespace SankalpAPI.DTO
{
    public class RegisterUserResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? UserId { get; set; }
    }
}
