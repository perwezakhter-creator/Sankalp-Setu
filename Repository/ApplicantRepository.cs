using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;

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

