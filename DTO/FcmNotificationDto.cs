using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SankalpAPI.DTO
{
    public class UserFcmDeviceDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("fcmToken")]
        public string FcmToken { get; set; } = string.Empty;

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = "Android";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [JsonPropertyName("updatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; }

        [JsonPropertyName("lastSeenAtUtc")]
        public DateTime? LastSeenAtUtc { get; set; }
    }

    public class SendUserNotificationRequest
    {
        [Required(ErrorMessage = "UserId is required.")]
        [StringLength(100, ErrorMessage = "UserId cannot exceed 100 characters.")]
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Body is required.")]
        [StringLength(4000, ErrorMessage = "Body cannot exceed 4000 characters.")]
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, string>? Data { get; set; }
    }

    public class SendUserNotificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UserId { get; set; }

        [JsonPropertyName("totalDevices")]
        public int TotalDevices { get; set; }

        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }

        [JsonPropertyName("failureCount")]
        public int FailureCount { get; set; }

        [JsonPropertyName("deactivatedTokenCount")]
        public int DeactivatedTokenCount { get; set; }
    }
}
