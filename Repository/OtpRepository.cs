using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;

namespace SankalpAPI.Repository
{
    public class OtpRepository : IOtpRepository
    {
        private readonly clsDataAccess _dataAccess;

        public OtpRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
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
    }
}
