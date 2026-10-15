using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SankalpAPI.Model
{
    /// <summary>
    /// An administrative Group backed by dbo.mst_portal.
    /// </summary>
    public class GroupDto
    {
        [JsonPropertyName("portal_id")]
        public long PortalId { get; set; }

        [JsonPropertyName("portal_name")]
        public string PortalName { get; set; } = string.Empty;

        [JsonPropertyName("portal_url")]
        public string? PortalUrl { get; set; }

        [JsonPropertyName("portal_image_path")]
        public string? PortalImagePath { get; set; }

        [JsonPropertyName("is_active")]
        public string IsActive { get; set; } = "Y";
    }

    /// <summary>
    /// An administrative Subgroup backed by dbo.UserType.
    /// UserTypeId is scoped to its parent portal.
    /// </summary>
    public class SubGroupDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("portal_id")]
        public long PortalId { get; set; }

        [JsonPropertyName("portal_name")]
        public string PortalName { get; set; } = string.Empty;

        [JsonPropertyName("user_type_id")]
        public long UserTypeId { get; set; }

        [JsonPropertyName("user_type_name")]
        public string UserTypeName { get; set; } = string.Empty;

        [JsonPropertyName("user_type_name_hn")]
        public string? UserTypeNameHn { get; set; }

        [JsonPropertyName("is_active")]
        public string IsActive { get; set; } = "Y";
    }

    public class GroupManagementListDto
    {
        [JsonPropertyName("groups")]
        public List<GroupDto> Groups { get; set; } = new();

        [JsonPropertyName("sub_groups")]
        public List<SubGroupDto> SubGroups { get; set; } = new();
    }

    public class CreateGroupRequest
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(255, ErrorMessage = "Group name cannot exceed 255 characters.")]
        [JsonPropertyName("portal_name")]
        public string PortalName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Portal URL cannot exceed 2000 characters.")]
        [Url(ErrorMessage = "Portal URL must be a valid absolute URL.")]
        [JsonPropertyName("portal_url")]
        public string? PortalUrl { get; set; }

        [StringLength(1000, ErrorMessage = "Image path cannot exceed 1000 characters.")]
        [JsonPropertyName("portal_image_path")]
        public string? PortalImagePath { get; set; }

        [RegularExpression("^[YNyn]$", ErrorMessage = "Status must be Y or N.")]
        [JsonPropertyName("is_active")]
        public string? IsActive { get; set; } = "Y";
    }

    public class CreateSubGroupRequest
    {
        [Range(1, long.MaxValue, ErrorMessage = "A parent group is required.")]
        [JsonPropertyName("portal_id")]
        public long PortalId { get; set; }

        [Required(ErrorMessage = "Subgroup name is required.")]
        [StringLength(255, ErrorMessage = "Subgroup name cannot exceed 255 characters.")]
        [JsonPropertyName("user_type_name")]
        public string UserTypeName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Hindi subgroup name cannot exceed 255 characters.")]
        [JsonPropertyName("user_type_name_hn")]
        public string? UserTypeNameHn { get; set; }

        [RegularExpression("^[YNyn]$", ErrorMessage = "Status must be Y or N.")]
        [JsonPropertyName("is_active")]
        public string? IsActive { get; set; } = "Y";
    }

    public class GroupManagementResponse<T>
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }
}
