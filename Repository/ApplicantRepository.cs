using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Security;

namespace SankalpAPI.Repository
{
    public class ApplicantRepository : IApplicantRepository
    {
        private readonly clsDataAccess _dataAccess;
        private readonly ExceptionLogging _exceptionLogging;

        public ApplicantRepository(clsDataAccess dataAccess, ExceptionLogging exceptionLogging)
        {
            _dataAccess = dataAccess;
            _exceptionLogging = exceptionLogging;
        }

        public Task<bool> SaveBeneficiaryDetailsAsync(long portalId, string mobileNo, string? deviceId, string? session, string? userType, List<BeneficiaryData> beneficiaries, string? rawJson = null)
        {
            if (beneficiaries == null || beneficiaries.Count == 0)
                return Task.FromResult(false);

            try
            {
                foreach (var b in beneficiaries)
                {
                    string safeBeneficiaryNumber = b.BeneficiaryID?.Trim() ?? string.Empty;
                    string safeName = b.Name?.Trim() ?? string.Empty;
                    string safeScheme = b.SchemeName?.Trim() ?? (b.SchemeCode?.Trim() ?? string.Empty);

                    SqlParameter[] parameters =
                    {
                        new SqlParameter("@p_portal_id", portalId),
                        new SqlParameter("@p_mobile_no", mobileNo.Trim()),
                        new SqlParameter("@p_beneficiary_number", string.IsNullOrEmpty(safeBeneficiaryNumber) ? (object)DBNull.Value : safeBeneficiaryNumber),
                        new SqlParameter("@p_beneficiary_name", string.IsNullOrEmpty(safeName) ? (object)DBNull.Value : safeName),
                        new SqlParameter("@p_name", string.IsNullOrEmpty(safeName) ? (object)DBNull.Value : safeName),
                        new SqlParameter("@p_scheme", string.IsNullOrEmpty(safeScheme) ? (object)DBNull.Value : safeScheme),
                        new SqlParameter("@p_block_name", string.IsNullOrEmpty(b.BlockName) ? (object)DBNull.Value : b.BlockName.Trim()),
                        new SqlParameter("@p_block_id", string.IsNullOrEmpty(b.BlockId) ? (object)DBNull.Value : b.BlockId.Trim()),
                        new SqlParameter("@p_district_name", string.IsNullOrEmpty(b.DistrictName) ? (object)DBNull.Value : b.DistrictName.Trim()),
                        new SqlParameter("@p_district_id", string.IsNullOrEmpty(b.DistrictId) ? (object)DBNull.Value : b.DistrictId.Trim()),
                        new SqlParameter("@p_otp", string.IsNullOrEmpty(b.OTP) ? (object)DBNull.Value : b.OTP.Trim()),
                        new SqlParameter("@p_device_id", string.IsNullOrEmpty(deviceId) ? (object)DBNull.Value : deviceId.Trim()),
                        new SqlParameter("@p_session", string.IsNullOrEmpty(session) ? (object)DBNull.Value : session.Trim()),
                        new SqlParameter("@p_raw_json", string.IsNullOrEmpty(rawJson) ? (object)DBNull.Value : rawJson),
                        new SqlParameter("@p_a_id", b.A_Id > 0 ? (object)b.A_Id : DBNull.Value),
                        new SqlParameter("@p_dob", string.IsNullOrEmpty(b.DOB) ? (object)DBNull.Value : b.DOB.Trim()),
                        new SqlParameter("@p_gender", string.IsNullOrEmpty(b.Gender) ? (object)DBNull.Value : b.Gender.Trim()),
                        new SqlParameter("@p_category_name", string.IsNullOrEmpty(b.CategoryName) ? (object)DBNull.Value : b.CategoryName.Trim()),
                        new SqlParameter("@p_scheme_code", string.IsNullOrEmpty(b.SchemeCode) ? (object)DBNull.Value : b.SchemeCode.Trim()),
                        new SqlParameter("@p_scheme_name", string.IsNullOrEmpty(b.SchemeName) ? (object)DBNull.Value : b.SchemeName.Trim()),
                        new SqlParameter("@p_area", string.IsNullOrEmpty(b.Area) ? (object)DBNull.Value : b.Area.Trim()),
                        new SqlParameter("@p_village_name", string.IsNullOrEmpty(b.VillageName) ? (object)DBNull.Value : b.VillageName.Trim()),
                        new SqlParameter("@p_village_code", string.IsNullOrEmpty(b.villageCode) ? (object)DBNull.Value : b.villageCode.Trim()),
                        new SqlParameter("@p_ward_name", string.IsNullOrEmpty(b.WardName) ? (object)DBNull.Value : b.WardName.Trim()),
                        new SqlParameter("@p_ward_code", string.IsNullOrEmpty(b.WardCode) ? (object)DBNull.Value : b.WardCode.Trim()),
                        new SqlParameter("@p_panchyat_code", string.IsNullOrEmpty(b.PanchyatCode) ? (object)DBNull.Value : b.PanchyatCode.Trim()),
                        new SqlParameter("@p_panchyat_name", string.IsNullOrEmpty(b.PanchyatName) ? (object)DBNull.Value : b.PanchyatName.Trim())
                    };

                    _dataAccess.ExecuteProcedure("sp_save_beneficiary_details", parameters);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(false);
            }
        }

        public async Task<(bool IsValid, string? UserId)> VerifyOtpAsync(long portalId, string mobileNo, string? deviceId, string otp, int expiryMinutes = 10, long? aId = null)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", portalId),
                    new SqlParameter("@p_mobile_no", mobileNo.Trim()),
                    new SqlParameter("@p_otp", otp.Trim()),
                    new SqlParameter("@p_device_id", string.IsNullOrWhiteSpace(deviceId) ? (object)DBNull.Value : deviceId.Trim()),
                    new SqlParameter("@p_expiry_mins", expiryMinutes)
                };

                DataTable dt = _dataAccess.GetDataTableWithProc("sp_verify_applicant_otp", parameters);
                if (dt.Rows.Count > 0)
                {
                    bool isValid = Convert.ToBoolean(dt.Rows[0]["is_valid"]);
                    string? userId = dt.Rows[0]["user_id"] != DBNull.Value ? dt.Rows[0]["user_id"]?.ToString() : null;

                    if (isValid && aId.HasValue && aId.Value > 0)
                    {
                        await UpdateBeneficiaryVerificationStatusAsync(aId.Value, "Y", deviceId?.ToString());
                    }

                    return (isValid, userId);
                }

                return (false, (string?)null);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return (false, (string?)null);
            }
        }

        public Task<bool> SaveSecretAndTokenAsync(long portalId, string mobileNo, string? deviceId, string? userId, string secretKey, string token, string? tokenHash)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", portalId),
                    new SqlParameter("@p_mobile_no", mobileNo.Trim()),
                    new SqlParameter("@p_device_id", string.IsNullOrWhiteSpace(deviceId) ? (object)DBNull.Value : deviceId.Trim()),
                    new SqlParameter("@p_userid", string.IsNullOrWhiteSpace(userId) ? (object)DBNull.Value : userId.Trim()),
                    new SqlParameter("@p_secretkey", secretKey.Trim()),
                    new SqlParameter("@p_token", token.Trim()),
                    new SqlParameter("@p_token_hash", string.IsNullOrWhiteSpace(tokenHash) ? (object)DBNull.Value : tokenHash.Trim())
                };

                DataTable dt = _dataAccess.GetDataTableWithProc("sp_save_user_portal_credentials", parameters);
                bool status = dt.Rows.Count > 0 && Convert.ToBoolean(dt.Rows[0]["status"]);
                return Task.FromResult(status);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(false);
            }
        }

        public Task<(long A_Id, string? Session, string? DeviceId, string? BeneficiaryNumber)> GetLatestBeneficiaryInfoAsync(long portalId, string mobileNo, long? aId = null)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", portalId),
                    new SqlParameter("@p_mobile_no", mobileNo.Trim()),
                    new SqlParameter("@p_a_id", (aId.HasValue && aId.Value > 0) ? (object)aId.Value : DBNull.Value)
                };

                DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_latest_beneficiary_info", parameters);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    long resAId = row["a_id"] != DBNull.Value ? Convert.ToInt64(row["a_id"]) : 0;
                    string? session = row["session"] != DBNull.Value ? row["session"]?.ToString() : null;
                    string? deviceId = row["device_id"] != DBNull.Value ? row["device_id"]?.ToString() : null;
                    string? benNo = row["beneficiary_number"] != DBNull.Value ? row["beneficiary_number"]?.ToString() : null;
                    return Task.FromResult((resAId, session, deviceId, benNo));
                }

                return Task.FromResult((0L, (string?)null, (string?)null, (string?)null));
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult((0L, (string?)null, (string?)null, (string?)null));
            }
        }

        public Task<bool> MarkOtpValidatedAsync(string mobileNo, string otp)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_mobile", mobileNo.Trim()),
                    new SqlParameter("@p_otp", otp.Trim())
                };

                _dataAccess.ExecuteProcedure("sp_mark_otp_validated", parameters);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(false);
            }
        }

        public Task<bool> UpdateBeneficiaryVerificationStatusAsync(long aId, string isVerified = "Y", string? DeviceId = null)
        {
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_a_id", aId),
                    new SqlParameter("@p_is_verified", isVerified.Trim()),
                    new SqlParameter("@p_device_id", string.IsNullOrWhiteSpace(DeviceId) ? (object)DBNull.Value : DeviceId.Trim())
                };

                const string sql = @"UPDATE dbo.tbl_beneficiary_details 
                                     SET is_verified = @p_is_verified, update_date = GETDATE() 
                                     WHERE a_id = @p_a_id 
                                       AND (@p_device_id IS NULL OR device_id = @p_device_id)";
                int rows = _dataAccess.ExecuteSql(sql, parameters);

                // Fallback: If device_id was provided but did not match (e.g. device_id mismatch in DB), update by a_id alone
                if (rows == 0 && !string.IsNullOrWhiteSpace(DeviceId))
                {
                    SqlParameter[] fallbackParams =
                    {
                        new SqlParameter("@p_a_id", aId),
                        new SqlParameter("@p_is_verified", isVerified.Trim())
                    };
                    const string fallbackSql = "UPDATE dbo.tbl_beneficiary_details SET is_verified = @p_is_verified, update_date = GETDATE() WHERE a_id = @p_a_id";
                    rows = _dataAccess.ExecuteSql(fallbackSql, fallbackParams);
                }

                return Task.FromResult(rows > 0);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(false);
            }
        }

        public Task<(long MsgId, long MsgLogId)> SaveBeneficiaryMessageAsync(long portalId, string mobileNo, string? deviceId, BeneficiaryMessageItem message)
        {
            if (message == null)
                return Task.FromResult((0L, 0L));

            try
            {
                string normMobile = NormalizeMobile(!string.IsNullOrWhiteSpace(message.MobNo) ? message.MobNo : mobileNo);
                string? safeDeviceId = !string.IsNullOrWhiteSpace(deviceId) ? deviceId.Trim() : (!string.IsNullOrWhiteSpace(normMobile) ? normMobile : null);

                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", portalId),
                    new SqlParameter("@p_mobile", string.IsNullOrEmpty(normMobile) ? (object)DBNull.Value : normMobile),
                    new SqlParameter("@p_message_head", string.IsNullOrEmpty(message.Message_Head) ? (object)DBNull.Value : Truncate(message.Message_Head, 255)),
                    new SqlParameter("@p_message_body", string.IsNullOrEmpty(message.Message_Body) ? string.Empty : message.Message_Body.Trim()),
                    new SqlParameter("@p_validity_in_minutes", (short)60),
                    new SqlParameter("@p_is_active", "Y"),
                    new SqlParameter("@p_interest_id", DBNull.Value),
                    new SqlParameter("@p_photo1path", string.IsNullOrEmpty(message.Pic_Path1) ? (object)DBNull.Value : Truncate(message.Pic_Path1, 255)),
                    new SqlParameter("@p_photo2path", string.IsNullOrEmpty(message.Pic_Path2) ? (object)DBNull.Value : Truncate(message.Pic_Path2, 255)),
                    new SqlParameter("@p_videopath", string.IsNullOrEmpty(message.Video_Path) ? (object)DBNull.Value : Truncate(message.Video_Path, 255)),
                    new SqlParameter("@p_documentpath", string.IsNullOrEmpty(message.Doc_Path) ? (object)DBNull.Value : Truncate(message.Doc_Path, 255)),
                    new SqlParameter("@p_youtubeurl", DBNull.Value),
                    new SqlParameter("@p_facebookurl", DBNull.Value),
                    new SqlParameter("@p_instagramurl", DBNull.Value),
                    new SqlParameter("@p_Xurl", DBNull.Value),
                    new SqlParameter("@p_entryby", string.IsNullOrEmpty(message.CreatedBy) ? "System" : Truncate(message.CreatedBy, 255)),
                    new SqlParameter("@p_entrydate", message.CreatedDate.HasValue ? (object)message.CreatedDate.Value : DateTime.Now),
                    new SqlParameter("@p_device_id", string.IsNullOrEmpty(safeDeviceId) ? (object)DBNull.Value : safeDeviceId),
                    new SqlParameter("@p_is_synced", "Yes")
                };

                DataTable dt = _dataAccess.GetDataTableWithProc("sp_save_beneficiary_message", parameters);
                if (dt.Rows.Count > 0)
                {
                    long msgId = dt.Rows[0]["msg_id"] != DBNull.Value ? Convert.ToInt64(dt.Rows[0]["msg_id"]) : 0;
                    long msgLogId = dt.Rows[0]["msg_log_id"] != DBNull.Value ? Convert.ToInt64(dt.Rows[0]["msg_log_id"]) : 0;
                    return Task.FromResult((msgId, msgLogId));
                }

                return Task.FromResult((0L, 0L));
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult((0L, 0L));
            }
        }

        public Task<string?> GetUserTokenAsync(long portalId, string mobileNo)
        {
            try
            {
                string normMobile = NormalizeMobile(mobileNo);
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", portalId),
                    new SqlParameter("@p_mobile", normMobile)
                };

                const string sql = @"SELECT TOP 1 token 
                                     FROM dbo.tbl_map_user_portal 
                                     WHERE portal_id = @p_portal_id 
                                       AND (mobile = @p_mobile OR mobile LIKE '%' + @p_mobile)
                                       AND token IS NOT NULL AND LEN(token) > 0
                                     ORDER BY user_portal_id DESC";

                object? tokenObj = _dataAccess.ExecuteScalar(sql, parameters);
                return Task.FromResult(tokenObj != null && tokenObj != DBNull.Value ? tokenObj.ToString() : null);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<(long PortalId, string? Mobile, string? DeviceId)> GetUserByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult((1L, (string?)null, (string?)null));

            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_token", token.Trim())
                };

                const string sql = @"
                    SELECT TOP 1 
                        m.portal_id, 
                        m.mobile, 
                        b.device_id
                    FROM dbo.tbl_map_user_portal m
                    LEFT JOIN dbo.tbl_beneficiary_details b 
                        ON b.portal_id = m.portal_id 
                       AND (b.mobile_no = m.mobile OR b.mobile_no LIKE '%' + m.mobile)
                    WHERE m.token = @p_token
                    ORDER BY m.user_portal_id DESC, b.id DESC";

                DataTable dt = _dataAccess.GetDataTable(sql, parameters);
                if (dt.Rows.Count > 0)
                {
                    long portalId = dt.Rows[0]["portal_id"] != DBNull.Value ? Convert.ToInt64(dt.Rows[0]["portal_id"]) : 1;
                    string? mobile = dt.Rows[0]["mobile"] != DBNull.Value ? dt.Rows[0]["mobile"]?.ToString() : null;
                    string? deviceId = dt.Rows[0]["device_id"] != DBNull.Value ? dt.Rows[0]["device_id"]?.ToString() : null;
                    return Task.FromResult((portalId, mobile, deviceId));
                }

                return Task.FromResult((1L, (string?)null, (string?)null));
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult((1L, (string?)null, (string?)null));
            }
        }

        public Task<bool> CheckApplicantPortalMappingExistsAsync(long portalId, string mobileNo, long? userTypeId)
        {
            try
            {
                string normMobile = NormalizeMobile(mobileNo);
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_portal_id", SqlDbType.BigInt) { Value = portalId },
                    new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile },
                    new SqlParameter("@p_user_type_id", SqlDbType.BigInt) { Value = userTypeId.HasValue ? (object)userTypeId.Value : DBNull.Value }
                };

                const string sql = @"
                    SELECT COUNT(1)
                    FROM dbo.tbl_map_user_portal
                    WHERE portal_id = @p_portal_id
                      AND mobile = @p_mobile
                      AND ISNULL(User_Type_ID, -1) = ISNULL(@p_user_type_id, -1);";

                object? res = _dataAccess.ExecuteScalar(sql, parameters);
                int count = res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
                return Task.FromResult(count > 0);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                throw;
            }
        }

        public Task<(bool IsAllowed, string? ErrorMessage)> CheckOtpSendRateLimitAsync(
            string mobileNo,
            int cooldownSeconds = 30,
            int maxRequestsInWindow = 5,
            int windowMinutes = 10)
        {
            try
            {
                string normMobile = NormalizeMobile(mobileNo);
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile },
                    new SqlParameter("@p_window_mins", SqlDbType.Int) { Value = windowMinutes }
                };

                const string sql = @"
                    SELECT
                        (SELECT TOP 1 DATEDIFF(SECOND, otpgeneratetime, GETDATE())
                         FROM dbo.tbl_otp
                         WHERE mobile = @p_mobile AND (isvalidate IS NULL OR isvalidate = 'N')
                         ORDER BY id DESC) AS SecondsSinceLastActiveOtp,
                        (SELECT COUNT(1)
                         FROM dbo.tbl_otp
                         WHERE mobile = @p_mobile
                           AND otpgeneratetime >= DATEADD(MINUTE, -@p_window_mins, GETDATE())) AS RecentRequestCount;";

                DataTable dt = _dataAccess.GetDataTable(sql, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    if (row["SecondsSinceLastActiveOtp"] != DBNull.Value)
                    {
                        int secondsSinceLast = Convert.ToInt32(row["SecondsSinceLastActiveOtp"]);
                        if (secondsSinceLast >= 0 && secondsSinceLast < cooldownSeconds)
                        {
                            int waitSeconds = cooldownSeconds - secondsSinceLast;
                            return Task.FromResult<(bool, string?)>((false, $"Please wait {waitSeconds} second(s) before requesting a new OTP."));
                        }
                    }

                    if (row["RecentRequestCount"] != DBNull.Value)
                    {
                        int recentCount = Convert.ToInt32(row["RecentRequestCount"]);
                        if (recentCount >= maxRequestsInWindow)
                        {
                            return Task.FromResult<(bool, string?)>((false, $"Too many OTP requests. Maximum {maxRequestsInWindow} OTP requests allowed within {windowMinutes} minutes."));
                        }
                    }
                }

                return Task.FromResult<(bool, string?)>((true, null));
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult<(bool, string?)>((true, null));
            }
        }

        public Task<bool> SaveRegistrationOtpAsync(
            long portalId,
            string mobileNo,
            string? deviceId,
            string? session,
            long? userTypeId,
            string otpHash)
        {
            string normMobile = NormalizeMobile(mobileNo);
            string encodedOtpPayload = BuildOtpPayload(otpHash, portalId, userTypeId, session, 0);

            using var con = _dataAccess.GetConnection();
            con.Open();
            using var trans = con.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                // 1. Invalidate any previous unverified OTPs for this mobile number
                const string invalidateSql = @"
                    UPDATE dbo.tbl_otp
                    SET isvalidate = 'E'
                    WHERE mobile = @p_mobile
                      AND (isvalidate IS NULL OR isvalidate = 'N');";

                using (var invCmd = new SqlCommand(invalidateSql, con, trans))
                {
                    invCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile });
                    invCmd.ExecuteNonQuery();
                }

                // 2. Insert new hashed OTP with bound registration context
                const string insertSql = @"
                    INSERT INTO dbo.tbl_otp (mobile, otp, isvalidate, otpgeneratetime, deviceid)
                    VALUES (@p_mobile, @p_otp, 'N', GETDATE(), @p_deviceid);";

                int rows;
                using (var insCmd = new SqlCommand(insertSql, con, trans))
                {
                    insCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile });
                    insCmd.Parameters.Add(new SqlParameter("@p_otp", SqlDbType.VarChar, 255) { Value = encodedOtpPayload });
                    insCmd.Parameters.Add(new SqlParameter("@p_deviceid", SqlDbType.VarChar, 255)
                    {
                        Value = string.IsNullOrWhiteSpace(deviceId) ? (object)DBNull.Value : Truncate(deviceId, 255)!
                    });
                    rows = insCmd.ExecuteNonQuery();
                }

                trans.Commit();
                return Task.FromResult(rows > 0);
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(false);
            }
        }

        public Task<long?> GetPendingOtpPortalIdAsync(string mobileNo, int expiryMinutes = 10)
        {
            try
            {
                string normMobile = NormalizeMobile(mobileNo);
                SqlParameter[] parameters =
                {
                    new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile },
                    new SqlParameter("@p_expiry_mins", SqlDbType.Int) { Value = expiryMinutes }
                };

                const string sql = @"
                    SELECT TOP 1 otp
                    FROM dbo.tbl_otp
                    WHERE mobile = @p_mobile
                      AND (isvalidate IS NULL OR isvalidate = 'N')
                      AND otpgeneratetime >= DATEADD(MINUTE, -@p_expiry_mins, GETDATE())
                    ORDER BY id DESC;";

                object? otpObj = _dataAccess.ExecuteScalar(sql, parameters);
                if (otpObj != null && otpObj != DBNull.Value)
                {
                    string rawPayload = otpObj.ToString() ?? string.Empty;
                    var parsed = ParseOtpPayload(rawPayload);
                    if (parsed.PortalId.HasValue && parsed.PortalId.Value > 0)
                    {
                        return Task.FromResult<long?>(parsed.PortalId.Value);
                    }
                }

                return Task.FromResult<long?>(null);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult<long?>(null);
            }
        }

        public Task<ApplicantOtpVerificationResult> VerifyOtpAndInsertPortalMappingAsync(
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
            string tokenHash)
        {
            string normMobile = NormalizeMobile(mobileNo);
            string cleanOtp = otpInput?.Trim() ?? string.Empty;

            using var con = _dataAccess.GetConnection();
            con.Open();
            using var trans = con.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // -----------------------------------------------------------------
                // 1. Fetch latest OTP record for this mobile number with row lock
                // -----------------------------------------------------------------
                const string selectOtpSql = @"
                    SELECT TOP 1
                        id,
                        mobile,
                        otp,
                        isvalidate,
                        otpgeneratetime,
                        otpvalidatetime,
                        deviceid,
                        CASE WHEN otpgeneratetime < DATEADD(MINUTE, -@p_expiry_mins, GETDATE()) THEN 1 ELSE 0 END AS is_expired
                    FROM dbo.tbl_otp WITH (UPDLOCK, HOLDLOCK)
                    WHERE mobile = @p_mobile
                    ORDER BY id DESC;";

                long otpId = 0;
                string rawOtpPayload = string.Empty;
                string isValidate = string.Empty;
                string? storedDeviceId = null;
                bool isExpired = false;

                using (var selCmd = new SqlCommand(selectOtpSql, con, trans))
                {
                    selCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile });
                    selCmd.Parameters.Add(new SqlParameter("@p_expiry_mins", SqlDbType.Int) { Value = expiryMinutes });

                    using var reader = selCmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        reader.Close();
                        trans.Rollback();
                        return Task.FromResult(new ApplicantOtpVerificationResult
                        {
                            Success = false,
                            StatusCode = (int)HttpStatusCode.BadRequest,
                            Message = "No OTP found for this mobile number. Please initiate registration to receive an OTP."
                        });
                    }

                    otpId = reader.GetInt64(reader.GetOrdinal("id"));
                    rawOtpPayload = reader["otp"] != DBNull.Value ? reader["otp"].ToString() ?? string.Empty : string.Empty;
                    isValidate = reader["isvalidate"] != DBNull.Value ? reader["isvalidate"].ToString()?.Trim() ?? "N" : "N";
                    storedDeviceId = reader["deviceid"] != DBNull.Value ? reader["deviceid"].ToString()?.Trim() : null;
                    isExpired = Convert.ToInt32(reader["is_expired"]) == 1;
                }

                // -----------------------------------------------------------------
                // 2. Check verification status (single-use / replay / lockout)
                // -----------------------------------------------------------------
                if (string.Equals(isValidate, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "OTP has already been used. Please request a new OTP."
                    });
                }

                if (string.Equals(isValidate, "X", StringComparison.OrdinalIgnoreCase))
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.TooManyRequests,
                        Message = "Maximum OTP verification attempts exceeded. Please request a new OTP."
                    });
                }

                if (string.Equals(isValidate, "E", StringComparison.OrdinalIgnoreCase))
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "OTP has expired or been superseded by a newer request. Please request a new OTP."
                    });
                }

                // -----------------------------------------------------------------
                // 3. Check OTP expiration window
                // -----------------------------------------------------------------
                if (isExpired)
                {
                    const string expireSql = "UPDATE dbo.tbl_otp SET isvalidate = 'E' WHERE id = @p_id;";
                    using (var expCmd = new SqlCommand(expireSql, con, trans))
                    {
                        expCmd.Parameters.Add(new SqlParameter("@p_id", SqlDbType.BigInt) { Value = otpId });
                        expCmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "OTP has expired. Please request a new OTP."
                    });
                }

                // -----------------------------------------------------------------
                // 4. Parse registration attempt metadata & check retry limit
                // -----------------------------------------------------------------
                var parsedMeta = ParseOtpPayload(rawOtpPayload);
                if (parsedMeta.RetryCount >= maxRetryAttempts)
                {
                    const string lockSql = "UPDATE dbo.tbl_otp SET isvalidate = 'X' WHERE id = @p_id;";
                    using (var lockCmd = new SqlCommand(lockSql, con, trans))
                    {
                        lockCmd.Parameters.Add(new SqlParameter("@p_id", SqlDbType.BigInt) { Value = otpId });
                        lockCmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.TooManyRequests,
                        Message = "Maximum OTP verification attempts exceeded. Please request a new OTP."
                    });
                }

                // -----------------------------------------------------------------
                // 5. Verify Portal, Device, and UserType bindings
                // -----------------------------------------------------------------
                if (parsedMeta.PortalId.HasValue && parsedMeta.PortalId.Value > 0 &&
                    requestedPortalId.HasValue && requestedPortalId.Value > 0 &&
                    parsedMeta.PortalId.Value != requestedPortalId.Value)
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Portal ID does not match the registration OTP attempt."
                    });
                }

                long effectivePortalId = (parsedMeta.PortalId.HasValue && parsedMeta.PortalId.Value > 0)
                    ? parsedMeta.PortalId.Value
                    : (requestedPortalId ?? 0);

                if (effectivePortalId <= 0)
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "A valid portal_id is required."
                    });
                }

                if (!string.IsNullOrWhiteSpace(storedDeviceId) &&
                    !string.IsNullOrWhiteSpace(requestedDeviceId) &&
                    !string.Equals(storedDeviceId, requestedDeviceId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "Device ID does not match the registration OTP attempt."
                    });
                }

                string? effectiveDeviceId = !string.IsNullOrWhiteSpace(requestedDeviceId)
                    ? requestedDeviceId.Trim()
                    : storedDeviceId;

                if (parsedMeta.UserTypeId.HasValue &&
                    requestedUserTypeId.HasValue &&
                    parsedMeta.UserTypeId.Value != requestedUserTypeId.Value)
                {
                    trans.Rollback();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "UserType does not match the registration OTP attempt."
                    });
                }

                long? effectiveUserTypeId = requestedUserTypeId ?? parsedMeta.UserTypeId;
                string? effectiveSession = !string.IsNullOrWhiteSpace(requestedSession) ? requestedSession.Trim() : parsedMeta.Session;

                // -----------------------------------------------------------------
                // 6. Verify OTP hash using constant-time comparison
                // -----------------------------------------------------------------
                string inputHash = ClientTokenHasher.Hash(cleanOtp);
                bool otpMatches = ConstantTimeEquals(parsedMeta.OtpHash, inputHash) ||
                                  (parsedMeta.OtpHash.Length != 64 && ConstantTimeEquals(parsedMeta.OtpHash, cleanOtp));

                if (!otpMatches)
                {
                    int newRetryCount = parsedMeta.RetryCount + 1;
                    bool lockOtp = newRetryCount >= maxRetryAttempts;
                    string updatedPayload = BuildOtpPayload(parsedMeta.OtpHash, effectivePortalId, effectiveUserTypeId, effectiveSession, newRetryCount);

                    const string updateRetrySql = @"
                        UPDATE dbo.tbl_otp
                        SET otp = @p_otp,
                            isvalidate = @p_isvalidate
                        WHERE id = @p_id;";

                    using (var retryCmd = new SqlCommand(updateRetrySql, con, trans))
                    {
                        retryCmd.Parameters.Add(new SqlParameter("@p_otp", SqlDbType.VarChar, 255) { Value = updatedPayload });
                        retryCmd.Parameters.Add(new SqlParameter("@p_isvalidate", SqlDbType.Char, 1) { Value = lockOtp ? "X" : "N" });
                        retryCmd.Parameters.Add(new SqlParameter("@p_id", SqlDbType.BigInt) { Value = otpId });
                        retryCmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return Task.FromResult(new ApplicantOtpVerificationResult
                    {
                        Success = false,
                        StatusCode = lockOtp ? (int)HttpStatusCode.TooManyRequests : (int)HttpStatusCode.BadRequest,
                        Message = lockOtp
                            ? "Invalid OTP. Maximum verification attempts exceeded. Please request a new OTP."
                            : "Invalid OTP."
                    });
                }

                // -----------------------------------------------------------------
                // 7. Prevent duplicate mapping in tbl_map_user_portal (with lock)
                // -----------------------------------------------------------------
                const string dupCheckSql = @"
                    SELECT COUNT(1)
                    FROM dbo.tbl_map_user_portal WITH (UPDLOCK, HOLDLOCK)
                    WHERE portal_id = @p_portal_id
                      AND mobile = @p_mobile
                      AND ISNULL(User_Type_ID, -1) = ISNULL(@p_user_type_id, -1);";

                using (var dupCmd = new SqlCommand(dupCheckSql, con, trans))
                {
                    dupCmd.Parameters.Add(new SqlParameter("@p_portal_id", SqlDbType.BigInt) { Value = effectivePortalId });
                    dupCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile });
                    dupCmd.Parameters.Add(new SqlParameter("@p_user_type_id", SqlDbType.BigInt)
                    {
                        Value = effectiveUserTypeId.HasValue ? (object)effectiveUserTypeId.Value : DBNull.Value
                    });

                    int existingCount = Convert.ToInt32(dupCmd.ExecuteScalar() ?? 0);
                    if (existingCount > 0)
                    {
                        trans.Rollback();
                        return Task.FromResult(new ApplicantOtpVerificationResult
                        {
                            Success = false,
                            StatusCode = (int)HttpStatusCode.Conflict,
                            Message = "Applicant is already registered and mapped with this portal for the specified user type."
                        });
                    }
                }

                // -----------------------------------------------------------------
                // 8. Mark OTP as verified in tbl_otp (single-use enforcement)
                // -----------------------------------------------------------------
                const string markVerifiedSql = @"
                    UPDATE dbo.tbl_otp
                    SET isvalidate = 'Y',
                        otpvalidatetime = GETDATE()
                    WHERE id = @p_id
                      AND (isvalidate IS NULL OR isvalidate = 'N');";

                using (var verifyCmd = new SqlCommand(markVerifiedSql, con, trans))
                {
                    verifyCmd.Parameters.Add(new SqlParameter("@p_id", SqlDbType.BigInt) { Value = otpId });
                    int updatedRows = verifyCmd.ExecuteNonQuery();
                    if (updatedRows != 1)
                    {
                        trans.Rollback();
                        return Task.FromResult(new ApplicantOtpVerificationResult
                        {
                            Success = false,
                            StatusCode = (int)HttpStatusCode.BadRequest,
                            Message = "OTP has already been verified. Please request a new OTP."
                        });
                    }
                }

                // -----------------------------------------------------------------
                // 9. Resolve userid, district_code, block_code, panchayatcode
                //    from existing applicant/user records (tbl_user, tbl_beneficiary_details, user_login, tbl_map_user_portal)
                // -----------------------------------------------------------------
                string? resolvedUserId = null;
                long? resolvedDistCode = null;
                long? resolvedBlockCode = null;
                long? resolvedPanchayatCode = null;
                string? fallbackDeviceId = null;

                // 9a. Primary lookup: dbo.tbl_user
                const string userLookupSql = @"
                    SELECT TOP 1
                        CAST(User_id AS VARCHAR(10)) AS user_id,
                        CAST(DistCode AS BIGINT) AS district_code,
                        CAST(BlockCode AS BIGINT) AS block_code,
                        CAST(PanchayatCode AS BIGINT) AS panchayat_code,
                        Deviceid AS device_id
                    FROM dbo.tbl_user
                    WHERE Mobile = @p_mobile
                    ORDER BY User_id DESC;";

                using (var userCmd = new SqlCommand(userLookupSql, con, trans))
                {
                    userCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 255) { Value = normMobile });
                    using var reader = userCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        resolvedUserId = reader["user_id"] != DBNull.Value ? reader["user_id"].ToString()?.Trim() : null;
                        if (reader["district_code"] != DBNull.Value) resolvedDistCode = Convert.ToInt64(reader["district_code"]);
                        if (reader["block_code"] != DBNull.Value) resolvedBlockCode = Convert.ToInt64(reader["block_code"]);
                        if (reader["panchayat_code"] != DBNull.Value) resolvedPanchayatCode = Convert.ToInt64(reader["panchayat_code"]);
                        fallbackDeviceId = reader["device_id"] != DBNull.Value ? reader["device_id"].ToString()?.Trim() : null;
                    }
                }

                // 9b. Secondary lookup: dbo.tbl_beneficiary_details (if any field is still missing)
                if (string.IsNullOrWhiteSpace(resolvedUserId) || !resolvedDistCode.HasValue || !resolvedBlockCode.HasValue || !resolvedPanchayatCode.HasValue)
                {
                    const string benLookupSql = @"
                        SELECT TOP 1
                            LEFT(LTRIM(RTRIM(beneficiary_number)), 10) AS user_id,
                            TRY_CAST(LTRIM(RTRIM(district_id)) AS BIGINT) AS district_code,
                            TRY_CAST(LTRIM(RTRIM(block_id)) AS BIGINT) AS block_code,
                            TRY_CAST(LTRIM(RTRIM(panchyat_code)) AS BIGINT) AS panchayat_code,
                            device_id
                        FROM dbo.tbl_beneficiary_details
                        WHERE mobile_no = @p_mobile
                        ORDER BY CASE WHEN portal_id = @p_portal_id THEN 0 ELSE 1 END, id DESC;";

                    using var benCmd = new SqlCommand(benLookupSql, con, trans);
                    benCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 20) { Value = normMobile });
                    benCmd.Parameters.Add(new SqlParameter("@p_portal_id", SqlDbType.BigInt) { Value = effectivePortalId });
                    using var reader = benCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        if (string.IsNullOrWhiteSpace(resolvedUserId) && reader["user_id"] != DBNull.Value)
                        {
                            string? benId = reader["user_id"].ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(benId)) resolvedUserId = benId;
                        }
                        if (!resolvedDistCode.HasValue && reader["district_code"] != DBNull.Value)
                            resolvedDistCode = Convert.ToInt64(reader["district_code"]);
                        if (!resolvedBlockCode.HasValue && reader["block_code"] != DBNull.Value)
                            resolvedBlockCode = Convert.ToInt64(reader["block_code"]);
                        if (!resolvedPanchayatCode.HasValue && reader["panchayat_code"] != DBNull.Value)
                            resolvedPanchayatCode = Convert.ToInt64(reader["panchayat_code"]);
                        if (string.IsNullOrWhiteSpace(fallbackDeviceId) && reader["device_id"] != DBNull.Value)
                            fallbackDeviceId = reader["device_id"].ToString()?.Trim();
                    }
                }

                // 9c. Tertiary lookup: dbo.user_login & existing dbo.tbl_map_user_portal
                if (string.IsNullOrWhiteSpace(resolvedUserId))
                {
                    const string loginLookupSql = @"
                        SELECT TOP 1 LEFT(LTRIM(RTRIM(userid)), 10) AS user_id
                        FROM dbo.user_login
                        WHERE contactno = @p_mobile OR userid = @p_mobile;";

                    using var loginCmd = new SqlCommand(loginLookupSql, con, trans);
                    loginCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 20) { Value = normMobile });
                    object? loginUserObj = loginCmd.ExecuteScalar();
                    if (loginUserObj != null && loginUserObj != DBNull.Value)
                    {
                        string? loginUid = loginUserObj.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(loginUid)) resolvedUserId = loginUid;
                    }
                }

                // Final fallback for userid according to sp_verify_applicant_otp convention: mobile number (10 digits)
                string finalUserId = !string.IsNullOrWhiteSpace(resolvedUserId)
                    ? Truncate(resolvedUserId, 10)!
                    : normMobile;

                string? finalDeviceId = !string.IsNullOrWhiteSpace(effectiveDeviceId)
                    ? Truncate(effectiveDeviceId, 255)
                    : Truncate(fallbackDeviceId, 255);

                // -----------------------------------------------------------------
                // 10. Insert into dbo.tbl_map_user_portal
                // -----------------------------------------------------------------
                const string insertMappingSql = @"
                    INSERT INTO dbo.tbl_map_user_portal (
                        portal_id,
                        User_Type_ID,
                        mobile,
                        secretkey,
                        token,
                        userid,
                        deviceid,
                        district_code,
                        block_code,
                        panchayatcode,
                        entry_by,
                        entry_date
                    )
                    OUTPUT INSERTED.user_portal_id
                    VALUES (
                        @p_portal_id,
                        @p_user_type_id,
                        @p_mobile,
                        @p_secretkey,
                        @p_token,
                        @p_userid,
                        @p_deviceid,
                        @p_district_code,
                        @p_block_code,
                        @p_panchayatcode,
                        @p_entry_by,
                        GETDATE()
                    );";

                long insertedUserPortalId;
                using (var insMapCmd = new SqlCommand(insertMappingSql, con, trans))
                {
                    insMapCmd.Parameters.Add(new SqlParameter("@p_portal_id", SqlDbType.BigInt) { Value = effectivePortalId });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_user_type_id", SqlDbType.BigInt)
                    {
                        Value = effectiveUserTypeId.HasValue ? (object)effectiveUserTypeId.Value : DBNull.Value
                    });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_mobile", SqlDbType.VarChar, 10) { Value = normMobile });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_secretkey", SqlDbType.VarChar, 255) { Value = secretKey.Trim() });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_token", SqlDbType.VarChar, 255) { Value = token.Trim() });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_userid", SqlDbType.VarChar, 10) { Value = finalUserId });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_deviceid", SqlDbType.VarChar, 255)
                    {
                        Value = string.IsNullOrWhiteSpace(finalDeviceId) ? (object)DBNull.Value : finalDeviceId
                    });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_district_code", SqlDbType.BigInt)
                    {
                        Value = resolvedDistCode.HasValue ? (object)resolvedDistCode.Value : DBNull.Value
                    });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_block_code", SqlDbType.BigInt)
                    {
                        Value = resolvedBlockCode.HasValue ? (object)resolvedBlockCode.Value : DBNull.Value
                    });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_panchayatcode", SqlDbType.BigInt)
                    {
                        Value = resolvedPanchayatCode.HasValue ? (object)resolvedPanchayatCode.Value : DBNull.Value
                    });
                    insMapCmd.Parameters.Add(new SqlParameter("@p_entry_by", SqlDbType.VarChar, 255) { Value = "ApplicantApi" });

                    object? idObj = insMapCmd.ExecuteScalar();
                    if (idObj == null || idObj == DBNull.Value)
                    {
                        trans.Rollback();
                        return Task.FromResult(new ApplicantOtpVerificationResult
                        {
                            Success = false,
                            StatusCode = (int)HttpStatusCode.InternalServerError,
                            Message = "Failed to insert applicant portal mapping into database."
                        });
                    }

                    insertedUserPortalId = Convert.ToInt64(idObj);
                }

                // -----------------------------------------------------------------
                // 11. Register client token in dbo.api_client_tokens
                // -----------------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(tokenHash) && tokenHash.Trim().Length == 64)
                {
                    const string saveTokenSql = @"
                        IF EXISTS (SELECT 1 FROM dbo.api_client_tokens WHERE token_hash = @p_token_hash)
                        BEGIN
                            UPDATE dbo.api_client_tokens
                            SET user_id = @p_user_id,
                                deviceid = @p_device_id,
                                is_active = 1,
                                created_date = GETDATE(),
                                expiry_date = DATEADD(HOUR, 8, GETDATE())
                            WHERE token_hash = @p_token_hash;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.api_client_tokens (user_id, deviceid, token_hash, created_date, expiry_date, is_active)
                            VALUES (@p_user_id, @p_device_id, @p_token_hash, GETDATE(), DATEADD(HOUR, 8, GETDATE()), 1);
                        END;";

                    using var tokenCmd = new SqlCommand(saveTokenSql, con, trans);
                    tokenCmd.Parameters.Add(new SqlParameter("@p_user_id", SqlDbType.VarChar, 100) { Value = finalUserId });
                    tokenCmd.Parameters.Add(new SqlParameter("@p_device_id", SqlDbType.VarChar, 250)
                    {
                        Value = string.IsNullOrWhiteSpace(finalDeviceId) ? (object)DBNull.Value : Truncate(finalDeviceId, 250)!
                    });
                    tokenCmd.Parameters.Add(new SqlParameter("@p_token_hash", SqlDbType.Char, 64) { Value = tokenHash.Trim() });
                    tokenCmd.ExecuteNonQuery();
                }

                // -----------------------------------------------------------------
                // 12. Commit transaction
                // -----------------------------------------------------------------
                trans.Commit();

                return Task.FromResult(new ApplicantOtpVerificationResult
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "OTP verified and applicant portal mapping completed successfully.",
                    Data = new ApplicantPortalRegistrationData
                    {
                        UserPortalId = insertedUserPortalId,
                        PortalId = effectivePortalId,
                        UserTypeId = effectiveUserTypeId,
                        Mobile = normMobile,
                        UserId = finalUserId,
                        Token = token.Trim(),
                        DeviceId = finalDeviceId,
                        DistrictCode = resolvedDistCode,
                        BlockCode = resolvedBlockCode,
                        PanchayatCode = resolvedPanchayatCode,
                        IsVerified = "Y",
                        VerifyDate = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                _exceptionLogging.SendErrorToText(ex);
                return Task.FromResult(new ApplicantOtpVerificationResult
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "A database error occurred while completing applicant registration and portal mapping."
                });
            }
        }

        private static string BuildOtpPayload(string otpHash, long portalId, long? userTypeId, string? session, int retryCount)
        {
            string cleanSession = string.IsNullOrWhiteSpace(session)
                ? string.Empty
                : session.Trim().Replace("|", string.Empty).Replace(":", string.Empty);

            if (cleanSession.Length > 20)
                cleanSession = cleanSession.Substring(0, 20);

            string userTypePart = userTypeId.HasValue ? userTypeId.Value.ToString() : string.Empty;
            return $"{otpHash}|P:{portalId}|U:{userTypePart}|S:{cleanSession}|R:{retryCount}";
        }

        private static (string OtpHash, long? PortalId, long? UserTypeId, string? Session, int RetryCount) ParseOtpPayload(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
                return (string.Empty, null, null, null, 0);

            string[] parts = rawPayload.Split('|', StringSplitOptions.RemoveEmptyEntries);
            string otpHash = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            long? portalId = null;
            long? userTypeId = null;
            string? session = null;
            int retryCount = 0;

            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
                {
                    if (long.TryParse(part.Substring(2), out long pid))
                        portalId = pid;
                }
                else if (part.StartsWith("U:", StringComparison.OrdinalIgnoreCase))
                {
                    string uVal = part.Substring(2);
                    if (!string.IsNullOrWhiteSpace(uVal) && long.TryParse(uVal, out long uid))
                        userTypeId = uid;
                }
                else if (part.StartsWith("S:", StringComparison.OrdinalIgnoreCase))
                {
                    string sVal = part.Substring(2);
                    if (!string.IsNullOrWhiteSpace(sVal))
                        session = sVal;
                }
                else if (part.StartsWith("R:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(part.Substring(2), out int rVal))
                        retryCount = rVal;
                }
            }

            return (otpHash, portalId, userTypeId, session, retryCount);
        }

        private static bool ConstantTimeEquals(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private static string NormalizeMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return string.Empty;
            var digits = new string(mobile.Where(char.IsDigit).ToArray());
            if (digits.Length > 10)
            {
                digits = digits.Substring(digits.Length - 10);
            }
            return digits;
        }

        private static string? Truncate(string? val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return null;
            val = val.Trim();
            return val.Length <= maxLen ? val : val.Substring(0, maxLen);
        }
    }
}

