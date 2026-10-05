using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SankalpAPI.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly clsDataAccess _dataAccess;

        public AuthRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task<UserDto?> LoginAsync(string userId, string password)
        {
            var enc = new Encryptor(Encryptor.PrivateKey.ToString());

            SqlParameter _UserID = new SqlParameter("@p_userid", userId);
            SqlParameter _Password = new SqlParameter("@p_psw", enc.Encrypt(password));

            SqlParameter[] parameters = { _UserID, _Password };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_login", parameters);

            if (dt.Rows.Count == 0)
            {
                return Task.FromResult<UserDto?>(null);
            }   

            DataRow row = dt.Rows[0];

            var user = new UserDto
            {
                UserID = GetColumnValue(row, "UserID"),

                Username = GetColumnValue(row, "Username"),

                UserRole = GetColumnValue(row, "UserRole"),

                Mobile = GetColumnValue(row, "ContactNo")
            };

            return Task.FromResult<UserDto?>(user);
        }

        private static string GetColumnValue(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return string.Empty;
            }

            if (row[columnName] == DBNull.Value)
            {
                return string.Empty;
            }

            return row[columnName]?.ToString() ?? string.Empty;
        }
    }
}
