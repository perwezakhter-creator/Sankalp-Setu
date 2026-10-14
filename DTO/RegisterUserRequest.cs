using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SankalpAPI.DTO
{
    public class RegisterUserRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be a valid 10-digit number.")]
        [JsonPropertyName("mobileNo")]
        public string MobileNo { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("interests")]
        public List<long> Interests { get; set; } = new List<long>();

        // Optional demographic fields matching actual tbl_user columns
        [JsonPropertyName("distCode")]
        public int? DistCode { get; set; }

        [JsonPropertyName("blockCode")]
        public int? BlockCode { get; set; }

        [JsonPropertyName("panchayatCode")]
        public int? PanchayatCode { get; set; }

        [JsonPropertyName("areaType")]
        public char? AreaType { get; set; }

        [JsonPropertyName("villCode")]
        public double? VillCode { get; set; }

        [JsonPropertyName("wardCode")]
        public string? WardCode { get; set; }

        [JsonPropertyName("genderCode")]
        public int? GenderCode { get; set; }

        [JsonPropertyName("ageGroupId")]
        public int? AgeGroupId { get; set; }

        [JsonPropertyName("categoryId")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("qualificationId")]
        public int? QualificationId { get; set; }

        [JsonPropertyName("occupationId")]
        public int? OccupationId { get; set; }

        [StringLength(200, ErrorMessage = "DeviceId cannot exceed 200 characters.")]
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [StringLength(2048, ErrorMessage = "FcmToken cannot exceed 2048 characters.")]
        [JsonPropertyName("fcmToken")]
        public string? FcmToken { get; set; }

        [StringLength(20, ErrorMessage = "Platform cannot exceed 20 characters.")]
        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("entryBy")]
        public string? EntryBy { get; set; }
    }
}
