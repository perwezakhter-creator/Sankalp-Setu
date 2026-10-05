using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;

namespace SankalpAPI.Repository
{
    public class TotpRepository : ITotpRepository
    {
        private readonly clsDataAccess _dataAccess;

        public TotpRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task<UserExistenceInfo> CheckUserExistsAsync(long portalId, string mobile, string? userId)
        {
            var info = new UserExistenceInfo
            {
                Exists = false,
                Mobile = mobile
            };

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId),
                new SqlParameter("@p_mobile", mobile),
                new SqlParameter("@p_userid", string.IsNullOrWhiteSpace(userId) ? (object)DBNull.Value : userId.Trim())
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_check_user_exists", parameters);
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                info.Exists = row["exists"] != DBNull.Value && Convert.ToBoolean(row["exists"]);
                info.Name = row["name"] != DBNull.Value ? row["name"]?.ToString() : null;
                info.UserId = row["user_id"] != DBNull.Value ? row["user_id"]?.ToString() : null;
            }

            return Task.FromResult(info);
        }

        public Task<bool> SaveOtpAsync(string mobile, string otp, string? deviceId)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_mobile", mobile),
                new SqlParameter("@p_otp", otp),
                new SqlParameter("@p_deviceid", (object?)deviceId ?? DBNull.Value)
            };

            int rows = _dataAccess.ExecuteProcedure("sp_save_otp", parameters);
            return Task.FromResult(rows > 0);
        }

        public Task<bool> ValidateOtpAsync(string mobile, string otp, int expiryMinutes)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_mobile", mobile),
                new SqlParameter("@p_otp", otp),
                new SqlParameter("@p_minutes", expiryMinutes)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_validate_totp_otp", parameters);
            if (dt.Rows.Count > 0 && dt.Rows[0]["is_valid"] != DBNull.Value)
            {
                return Task.FromResult(Convert.ToBoolean(dt.Rows[0]["is_valid"]));
            }

            return Task.FromResult(false);
        }

        public Task<bool> SaveTotpSecretAsync(long portalId, string mobile, string? userId, string secretKey, string? token, string entryBy)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId),
                new SqlParameter("@p_mobile", mobile),
                new SqlParameter("@p_secretkey", secretKey),
                new SqlParameter("@p_token", (object?)token ?? DBNull.Value),
                new SqlParameter("@p_userid", (object?)userId ?? DBNull.Value),
                new SqlParameter("@p_entry_by", entryBy)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_save_totp_secret", parameters);
            if (dt.Rows.Count > 0 && dt.Columns.Contains("status"))
            {
                return Task.FromResult(Convert.ToBoolean(dt.Rows[0]["status"]));
            }

            return Task.FromResult(false);
        }

        public Task<string?> GetTotpSecretAsync(long portalId, string mobile)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId),
                new SqlParameter("@p_mobile", mobile)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_totp_secret", parameters);

            if (dt.Rows.Count == 0 || dt.Rows[0]["secretkey"] == DBNull.Value)
            {
                return Task.FromResult<string?>(null);
            }

            string? secret = dt.Rows[0]["secretkey"]?.ToString();
            return Task.FromResult(secret);
        }
    }
}
