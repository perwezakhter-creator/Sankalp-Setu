using SankalpAPI.Helper;
using SankalpAPI.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SankalpAPI.Repository
{
    public class ClientTokenRepository : IClientTokenRepository
    {
        private readonly clsDataAccess _dataAccess;

        public ClientTokenRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task SaveTokenAsync(string userId, string tokenHash)
        {
            SqlParameter userIdParameter =
                new SqlParameter("@p_user_id", SqlDbType.VarChar, 100)
                {
                    Value = userId
                };

            SqlParameter tokenHashParameter =
                new SqlParameter("@p_token_hash", SqlDbType.Char, 64)
                {
                    Value = tokenHash
                };

            SqlParameter[] parameters =
            {
                userIdParameter,
                tokenHashParameter
            };

            _dataAccess.GetDataTableWithProc("sp_save_client_token", parameters);

            return Task.CompletedTask;
        }

        public Task<string?> ValidateTokenAsync(string tokenHash)
        {
            SqlParameter tokenHashParameter =
                new SqlParameter("@p_token_hash", SqlDbType.Char, 64)
                {
                    Value = tokenHash
                };

            SqlParameter[] parameters =
            {
                tokenHashParameter
            };

            DataTable dt =
                _dataAccess.GetDataTableWithProc("sp_validate_client_token", parameters);

            if (dt == null || dt.Rows.Count == 0)
            {
                return Task.FromResult<string?>(null);
            }

            DataRow row = dt.Rows[0];

            string columnName = dt.Columns.Contains("user_id") ? "user_id" : (dt.Columns.Contains("UserID") ? "UserID" : "");
            if (string.IsNullOrEmpty(columnName) || row[columnName] == DBNull.Value)
            {
                return Task.FromResult<string?>(null);
            }

            string userId = row[columnName]?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(userId);
        }

        public Task<bool> RevokeTokenAsync(string tokenHash)
        {
            SqlParameter tokenHashParameter =
                new SqlParameter("@p_token_hash", SqlDbType.Char, 64)
                {
                    Value = tokenHash
                };

            SqlParameter[] parameters =
            {
                tokenHashParameter
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_revoke_client_token", parameters);

            if (dt == null || dt.Rows.Count == 0)
            {
                return Task.FromResult(false);
            }

            if (!dt.Columns.Contains("RowsAffected") ||
                dt.Rows[0]["RowsAffected"] == DBNull.Value)
            {
                return Task.FromResult(false);
            }

            int rowsAffected =
                Convert.ToInt32(dt.Rows[0]["RowsAffected"]);

            return Task.FromResult(rowsAffected > 0);
        }
    }
}