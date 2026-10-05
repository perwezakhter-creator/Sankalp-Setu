using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SankalpAPI.Model
{
    /// <summary>
    /// Request model for fetching applicant/beneficiary details from an external portal API.
    /// </summary>
    public class ApplicantRequest
    {
        /// <summary>
        /// Portal identifier from mst_portal table.
        /// </summary>
        /// <example>1</example>
        [JsonPropertyName("portal_id")]
        [JsonProperty("portal_id")]
        public long PortalId { get; set; }

        /// <summary>
        /// Registered 10-digit mobile number of the applicant.
        /// </summary>
        /// <example>9876543210</example>
        [JsonPropertyName("mobile_no")]
        [JsonProperty("mobile_no")]
        public string MobileNo { get; set; } = string.Empty;

        /// <summary>
        /// Unique installation/device identifier.
        /// </summary>
        /// <example>DEVICE123456</example>
        [JsonPropertyName("deviceid")]
        [JsonProperty("deviceid")]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Optional Financial Year (e.g. "2024-2025"), Academic Year, or Session Token.
        /// </summary>
        /// <example>2024-2025</example>
        [JsonPropertyName("session")]
        [JsonProperty("session")]
        public string? Session { get; set; }

        [JsonPropertyName("UserType")]
        [JsonProperty("UserType")]
        public string? UserType { get; set; }
    }

    /// <summary>
    /// Beneficiary details returned from the external portal API.
    /// </summary>
    public class BeneficiaryData
    {
        [JsonPropertyName("a_Id")]
        [JsonProperty("a_Id")]
        public long A_Id { get; set; }

        [JsonPropertyName("beneficiaryID")]
        [JsonProperty("beneficiaryID")]
        public string? BeneficiaryID { get; set; }

        [JsonPropertyName("name")]
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonPropertyName("dob")]
        [JsonProperty("dob")]
        public string? DOB { get; set; }

        [JsonPropertyName("gender")]
        [JsonProperty("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("mobileNO")]
        [JsonProperty("mobileNO")]
        public string? MobileNO { get; set; }

        [JsonPropertyName("categoryName")]
        [JsonProperty("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("schemeCode")]
        [JsonProperty("schemeCode")]
        public string? SchemeCode { get; set; }

        [JsonPropertyName("schemeName")]
        [JsonProperty("schemeName")]
        public string? SchemeName { get; set; }

        [JsonPropertyName("area")]
        [JsonProperty("area")]
        public string? Area { get; set; }

        [JsonPropertyName("villageName")]
        [JsonProperty("villageName")]
        public string? VillageName { get; set; }

        [JsonPropertyName("villageCode")]
        [JsonProperty("villageCode")]
        public string? villageCode { get; set; }

        [JsonPropertyName("wardName")]
        [JsonProperty("wardName")]
        public string? WardName { get; set; }

        [JsonPropertyName("wardCode")]
        [JsonProperty("wardCode")]
        public string? WardCode { get; set; }

        [JsonPropertyName("panchyatCode")]
        [JsonProperty("panchyatCode")]
        public string? PanchyatCode { get; set; }

        [JsonPropertyName("panchyatName")]
        [JsonProperty("panchyatName")]
        public string? PanchyatName { get; set; }

        [JsonPropertyName("blockName")]
        [JsonProperty("blockName")]
        public string? BlockName { get; set; }

        [JsonPropertyName("blockId")]
        [JsonProperty("blockId")]
        public string? BlockId { get; set; }

        [JsonPropertyName("districtName")]
        [JsonProperty("districtName")]
        public string? DistrictName { get; set; }

        [JsonPropertyName("districtId")]
        [JsonProperty("districtId")]
        public string? DistrictId { get; set; }

        [JsonPropertyName("otp")]
        [JsonProperty("otp")]
        public string? OTP { get; set; }

        [JsonPropertyName("is_verified")]
        [JsonProperty("is_verified")]
        public string? IsVerified { get; set; }
    }

    /// <summary>
    /// Backward compatible alias for BeneficiaryData.
    /// </summary>
    public class BeneficiaryDetailDto : BeneficiaryData { }

    /// <summary>
    /// Response model returning data in standard { status, message, data } format.
    /// </summary>
    public class ApplicantResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public object? Data { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int StatusCode { get; set; } = 200;
    }

    /// <summary>
    /// Schema documentation model for Swagger describing the list format in data.
    /// </summary>
    public class ApplicantListResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public List<BeneficiaryData> Data { get; set; } = new List<BeneficiaryData>();
    }

    /// <summary>
    /// Request model for verifying OTP.
    /// Supports a_Id as a single number (23) or an array of numbers ([23, 24]).
    /// </summary>
    public class VerifyOtpRequest
    {
        [JsonPropertyName("a_Id")]
        [JsonProperty("a_Id")]
        [System.Text.Json.Serialization.JsonConverter(typeof(SankalpAPI.Helper.FlexibleLongListConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(SankalpAPI.Helper.FlexibleNewtonsoftLongListConverter))]
        public List<long> A_Id { get; set; } = new List<long>();

        [JsonPropertyName("mobileNo")]
        [JsonProperty("mobileNo")]
        public string MobileNo { get; set; } = string.Empty;

        [JsonPropertyName("otp")]
        [JsonProperty("otp")]
        public string Otp { get; set; } = string.Empty;

        [JsonPropertyName("deviceId")]
        [JsonProperty("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("session")]
        [JsonProperty("session")]
        public string? Session { get; set; }

        [JsonPropertyName("portal_id")]
        [JsonProperty("portal_id")]
        public long PortalId { get; set; } = 1;

        [JsonPropertyName("client_key")]
        [JsonProperty("client_key")]
        public string? ClientKey { get; set; }

        [JsonPropertyName("user_id")]
        [JsonProperty("user_id")]
        public string? UserId { get; set; }
    }

    /// <summary>
    /// Payload returned after verifying OTP and generating secret key & token.
    /// </summary>
    public class VerifyOtpData
    {
        [JsonPropertyName("a_Id")]
        [JsonProperty("a_Id")]
        public long A_Id { get; set; }

        [JsonPropertyName("beneficiaryID")]
        [JsonProperty("beneficiaryID")]
        public string? BeneficiaryID { get; set; }

        [JsonPropertyName("mobileNo")]
        [JsonProperty("mobileNo")]
        public string MobileNo { get; set; } = string.Empty;

        [JsonPropertyName("secretKey")]
        [JsonProperty("secretKey")]
        public string SecretKey { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("verifyDate")]
        [JsonProperty("verifyDate")]
        public DateTime? VerifyDate { get; set; }

        [JsonPropertyName("otpauth_url")]
        [JsonProperty("otpauth_url")]
        public string? OtpauthUrl { get; set; }

        [JsonPropertyName("portal_id")]
        [JsonProperty("portal_id")]
        public long PortalId { get; set; } = 1;

        [JsonPropertyName("user_id")]
        [JsonProperty("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("is_verified")]
        [JsonProperty("is_verified")]
        public string IsVerified { get; set; } = "Y";
    }

    /// <summary>
    /// Model representing the response from external portal verify-otp API.
    /// </summary>
    public class ExternalVerifyOtpResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public ExternalVerifyOtpData? Data { get; set; }
    }

    /// <summary>
    /// Model representing the payload inside external portal verify-otp response data.
    /// </summary>
    public class ExternalVerifyOtpData
    {
        [JsonPropertyName("a_Id")]
        [JsonProperty("a_Id")]
        public long A_Id { get; set; }

        [JsonPropertyName("beneficiaryID")]
        [JsonProperty("beneficiaryID")]
        public string? BeneficiaryID { get; set; }

        [JsonPropertyName("mobileNo")]
        [JsonProperty("mobileNo")]
        public string? MobileNo { get; set; }

        [JsonPropertyName("secretKey")]
        [JsonProperty("secretKey")]
        public string? SecretKey { get; set; }

        [JsonPropertyName("token")]
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonPropertyName("verifyDate")]
        [JsonProperty("verifyDate")]
        public DateTime? VerifyDate { get; set; }
    }

    /// <summary>
    /// Swagger schema documentation model for VerifyOtp response.
    /// </summary>
    public class VerifyOtpResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public VerifyOtpData? Data { get; set; }
    }

    /// <summary>
    /// Model representing a single beneficiary message retrieved from external portal API (eLabharthi).
    /// </summary>
    public class BeneficiaryMessageItem
    {
        [JsonPropertyName("message_Id")]
        [JsonProperty("message_Id")]
        public long Message_Id { get; set; }

        [JsonPropertyName("benificiary_No")]
        [JsonProperty("benificiary_No")]
        public string? Benificiary_No { get; set; }

        [JsonPropertyName("mobNo")]
        [JsonProperty("mobNo")]
        public string? MobNo { get; set; }

        [JsonPropertyName("message_Head")]
        [JsonProperty("message_Head")]
        public string? Message_Head { get; set; }

        [JsonPropertyName("message_Body")]
        [JsonProperty("message_Body")]
        public string? Message_Body { get; set; }

        [JsonPropertyName("type")]
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonPropertyName("pic_Path1")]
        [JsonProperty("pic_Path1")]
        public string? Pic_Path1 { get; set; }

        [JsonPropertyName("pic_Path2")]
        [JsonProperty("pic_Path2")]
        public string? Pic_Path2 { get; set; }

        [JsonPropertyName("video_Path")]
        [JsonProperty("video_Path")]
        public string? Video_Path { get; set; }

        [JsonPropertyName("doc_Path")]
        [JsonProperty("doc_Path")]
        public string? Doc_Path { get; set; }

        [JsonPropertyName("createdBy")]
        [JsonProperty("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("createdDate")]
        [JsonProperty("createdDate")]
        public DateTime? CreatedDate { get; set; }
    }

    /// <summary>
    /// Payload returned by external eLabharthi API for Get_Beneficiary_Messages.
    /// </summary>
    public class ExternalBeneficiaryMessageResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public List<BeneficiaryMessageItem>? Data { get; set; }
    }

    /// <summary>
    /// Request model for fetching beneficiary messages from eLabharthi API and syncing to database.
    /// </summary>
    public class GetBeneficiaryMessagesRequest
    {
        /// <summary>
        /// Portal identifier from mst_portal table.
        /// </summary>
        /// <example>1</example>
        [JsonPropertyName("portal_id")]
        [JsonProperty("portal_id")]
        public long PortalId { get; set; }
        /// <summary>
        /// Authorization token for accessing Get_Beneficiary_Messages API.
        /// </summary>
        /// <example>408C6E3F4D04A7F7C1608193407356D8</example>
        [JsonPropertyName("token")]
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Saved beneficiary message record with local database identifiers.
    /// </summary>
    public class SavedBeneficiaryMessageDto : BeneficiaryMessageItem
    {
        [JsonPropertyName("msg_id")]
        [JsonProperty("msg_id")]
        public long MsgId { get; set; }

        [JsonPropertyName("msg_log_id")]
        [JsonProperty("msg_log_id")]
        public long MsgLogId { get; set; }

        [JsonPropertyName("portal_id")]
        [JsonProperty("portal_id")]
        public long PortalId { get; set; }

        [JsonPropertyName("device_id")]
        [JsonProperty("device_id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("is_synced")]
        [JsonProperty("is_synced")]
        public string IsSynced { get; set; } = "Yes";

        [JsonPropertyName("sync_date")]
        [JsonProperty("sync_date")]
        public DateTime? SyncDate { get; set; }
    }

    /// <summary>
    /// Response model for GetBeneficiaryMessages.
    /// </summary>
    public class GetBeneficiaryMessagesResponse
    {
        [JsonPropertyName("status")]
        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public List<SavedBeneficiaryMessageDto> Data { get; set; } = new List<SavedBeneficiaryMessageDto>();
    }
}
