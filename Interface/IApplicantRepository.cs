using System.Collections.Generic;
using System.Threading.Tasks;
using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IApplicantRepository
    {
        /// <summary>
        /// Saves or updates beneficiary/applicant records returned from external portal into the database.
        /// </summary>
        Task<bool> SaveBeneficiaryDetailsAsync(long portalId, string mobileNo,string? deviceId, string? session, string? userType, List<BeneficiaryData> beneficiaries, string? rawJson = null);

        /// <summary>
        /// Validates OTP against tbl_otp and tbl_beneficiary_details.
        /// </summary>
        Task<(bool IsValid, string? UserId)> VerifyOtpAsync(long portalId, string mobileNo, string? deviceId, string otp, int expiryMinutes = 10, long? aId = null);

        /// <summary>
        /// Saves secret key and token in tbl_map_user_portal and registers token in tbl_client_token.
        /// </summary>
        Task<bool> SaveSecretAndTokenAsync(long portalId, string mobileNo, string? deviceId, string? userId, string secretKey, string token, string? tokenHash);

        /// <summary>
        /// Retrieves the latest beneficiary record information (a_id, session, device_id, beneficiary_number)
        /// previously saved for the given portal and mobile number.
        /// </summary>
        Task<(long A_Id, string? Session, string? DeviceId, string? BeneficiaryNumber)> GetLatestBeneficiaryInfoAsync(long portalId, string mobileNo, long? aId = null);

        /// <summary>
        /// Marks OTP as validated in tbl_otp.
        /// </summary>
        Task<bool> MarkOtpValidatedAsync(string mobileNo, string otp);

        /// <summary>
        /// Updates the verification status (is_verified) in tbl_beneficiary_details for the given a_id.
        /// </summary>
        Task<bool> UpdateBeneficiaryVerificationStatusAsync(long aId, string isVerified = "Y", string? DeviceId = null);

        /// <summary>
        /// Saves or updates a beneficiary message in tbl_Message (with mobile) and logs its sync status in tbl_Message_log.
        /// </summary>
        Task<(long MsgId, long MsgLogId)> SaveBeneficiaryMessageAsync(long portalId, string mobileNo, string? deviceId, BeneficiaryMessageItem message);

        /// <summary>
        /// Retrieves the authorization token for a user from tbl_map_user_portal.
        /// </summary>
        Task<string?> GetUserTokenAsync(long portalId, string mobileNo);

        /// <summary>
        /// Resolves portal ID, mobile number, and device ID associated with a user token.
        /// </summary>
        Task<(long PortalId, string? Mobile, string? DeviceId)> GetUserByTokenAsync(string token);

        /// <summary>
        /// Checks whether an applicant is already mapped to the specified portal and user type in tbl_map_user_portal.
        /// </summary>
        Task<bool> CheckApplicantPortalMappingExistsAsync(long portalId, string mobileNo, long? userTypeId);

        /// <summary>
        /// Checks OTP resend cooldown and rate limits for the given mobile number in tbl_otp.
        /// </summary>
        Task<(bool IsAllowed, string? ErrorMessage)> CheckOtpSendRateLimitAsync(
            string mobileNo,
            int cooldownSeconds = 30,
            int maxRequestsInWindow = 5,
            int windowMinutes = 10);

        /// <summary>
        /// Stores a hashed OTP and its registration context (portalId, userTypeId, session, deviceId) in tbl_otp,
        /// invalidating any prior unverified OTPs for the same mobile number.
        /// </summary>
        Task<bool> SaveRegistrationOtpAsync(
            long portalId,
            string mobileNo,
            string? deviceId,
            string? session,
            long? userTypeId,
            string otpHash);

        /// <summary>
        /// Resolves the portal_id associated with the latest pending OTP registration attempt for the mobile number, if any.
        /// </summary>
        Task<long?> GetPendingOtpPortalIdAsync(string mobileNo, int expiryMinutes = 10);

        /// <summary>
        /// Atomically verifies the OTP (checking expiration, single-use status, retry limit, and binding),
        /// marks the OTP as verified in tbl_otp, resolves existing applicant details (userid, district_code, block_code, panchayatcode),
        /// prevents duplicate mappings, inserts into tbl_map_user_portal, and registers the token hash in api_client_tokens.
        /// </summary>
        Task<ApplicantOtpVerificationResult> VerifyOtpAndInsertPortalMappingAsync(
            long? requestedPortalId,
            string mobileNo,
            string otpInput,
            string? requestedDeviceId,
            string? requestedSession,
            long? requestedUserTypeId,
            int expiryMinutes,
            int maxRetryAttempts,
            string secretKey,
            string token,
            string tokenHash);
    }
}
