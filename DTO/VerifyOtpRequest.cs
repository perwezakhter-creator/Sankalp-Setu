using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SankalpAPI.DTO
{
    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be a valid 10-digit number.")]  

        [JsonPropertyName("mobileNo")]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required.")]
        [JsonPropertyName("otp")]
        public string Otp { get; set; } = string.Empty;

        [JsonPropertyName("DeviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("clientKey")]
        public string? ClientKey { get; set; }
    }
}
