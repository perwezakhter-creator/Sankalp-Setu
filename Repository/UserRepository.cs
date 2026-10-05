using Microsoft.Data.SqlClient;
using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Security;
using SankalpAPI.Services;
using System.Data;

namespace SankalpAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly clsDataAccess _dataAccess;
        private readonly IConfiguration _config;
        private readonly ILogger<UserRepository> _logger;
        private readonly ClientTokenService _clientTokenService;
        private readonly ExceptionLogging _exceptionLogging;

        public UserRepository(clsDataAccess dataAccess, IConfiguration config, ClientTokenService clientTokenService, ILogger<UserRepository> logger, ExceptionLogging exceptionLogging)
        {
            _dataAccess = dataAccess;
            _config = config;
            _clientTokenService = clientTokenService;
            _logger = logger;
            _exceptionLogging = exceptionLogging;
        }

        // ============================================================
        // CHECK IF MOBILE EXISTS
        // ============================================================
        public Task<bool> IsMobileExistsAsync(string mobileNo, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.tbl_user WHERE Mobile = @Mobile;";
            SqlParameter _MobileNo = new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = mobileNo?.Trim() ?? string.Empty };
            SqlParameter[] parameters = { _MobileNo };
            DataTable dt = _dataAccess.GetDataTable(sql, parameters);

            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                return Task.FromResult(Convert.ToInt32(dt.Rows[0][0]) > 0);
            }

            return Task.FromResult(false);
        }

        // ============================================================
        // REGISTER USER WITH INTERESTS (ATOMIC SQL SERVER TRANSACTION)
        // ============================================================
        public async Task<(bool isSuccess, bool isDuplicate, long userId, string? errorMessage)> RegisterUserWithInterestsAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            try
            {
                // ----------------------------------------------------
                // STEP 1: Duplicate check using _dataAccess.GetDataTable
                // ----------------------------------------------------
                const string checkMobileSql = "SELECT COUNT(1) FROM dbo.tbl_user WHERE Mobile = @Mobile;";
                SqlParameter[] checkParams =
                {
                    new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = request.MobileNo.Trim() }
                };

                DataTable checkDt = _dataAccess.GetDataTable(checkMobileSql, checkParams);
                if (checkDt != null && checkDt.Rows.Count > 0 && Convert.ToInt32(checkDt.Rows[0][0]) > 0)
                {
                    return (isSuccess: false, isDuplicate: true, userId: 0, errorMessage: "Mobile number already registered.");
                }

                string clientToken = _clientTokenService.GenerateToken();               

                string tokenHash = ClientTokenHasher.Hash(clientToken);

                // ----------------------------------------------------
                // STEP 2: Insert into tbl_user & retrieve generated User_id
                // ----------------------------------------------------
                const string insertUserSql = @"INSERT INTO dbo.tbl_user (Name, Mobile, Password, DistCode, BlockCode, PanchayatCode, AreaType, VILLCODE, WARDCODE, Gender_code, Age_GroupID, Category_Id,
                                               Qualification_id, Occupation_id, Deviceid, Token, Entryby, Entrydate)
                                               OUTPUT INSERTED.User_id
                                               VALUES (@Name, @Mobile, @Password, @DistCode, @BlockCode, @PanchayatCode, @AreaType, @VILLCODE, @WARDCODE, @Gender_code, @Age_GroupID, @Category_Id,
                                               @Qualification_id, @Occupation_id, @Deviceid, @Token, @Entryby, GETDATE());";

                SqlParameter[] userParams =
                {
                    new SqlParameter("@Name", SqlDbType.VarChar, 255) { Value = request.Name.Trim() },
                    new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = request.MobileNo.Trim() },
                    new SqlParameter("@Password", SqlDbType.VarChar, 255) { Value = (object?)request.Password ?? string.Empty },
                    new SqlParameter("@DistCode", SqlDbType.Int) { Value = (object?)request.DistCode ?? DBNull.Value },
                    new SqlParameter("@BlockCode", SqlDbType.Int) { Value = (object?)request.BlockCode ?? DBNull.Value },
                    new SqlParameter("@PanchayatCode", SqlDbType.Int) { Value = (object?)request.PanchayatCode ?? DBNull.Value },
                    new SqlParameter("@AreaType", SqlDbType.Char, 1) { Value = request.AreaType.HasValue ? (object)request.AreaType.Value.ToString() : DBNull.Value },
                    new SqlParameter("@VILLCODE", SqlDbType.Float) { Value = (object?)request.VillCode ?? DBNull.Value },
                    new SqlParameter("@WARDCODE", SqlDbType.NVarChar, 50) { Value = (object?)request.WardCode ?? DBNull.Value },
                    new SqlParameter("@Gender_code", SqlDbType.Int) { Value = (object?)request.GenderCode ?? DBNull.Value },
                    new SqlParameter("@Age_GroupID", SqlDbType.Int) { Value = (object?)request.AgeGroupId ?? DBNull.Value },
                    new SqlParameter("@Category_Id", SqlDbType.Int) { Value = (object?)request.CategoryId ?? DBNull.Value },
                    new SqlParameter("@Qualification_id", SqlDbType.Int) { Value = (object?)request.QualificationId ?? DBNull.Value },
                    new SqlParameter("@Occupation_id", SqlDbType.Int) { Value = (object?)request.OccupationId ?? DBNull.Value },
                    new SqlParameter("@Deviceid", SqlDbType.VarChar, 255) { Value = (object?)request.DeviceId ?? DBNull.Value },
                    new SqlParameter("@Token", SqlDbType.VarChar, 255) { Value = (object?)clientToken ?? DBNull.Value },
                    new SqlParameter("@Entryby", SqlDbType.VarChar, 255) { Value = (object?)request.EntryBy ?? "System" }
                };

                DataTable userDt = _dataAccess.GetDataTable(insertUserSql, userParams);

                if (userDt == null || userDt.Rows.Count == 0 || userDt.Rows[0][0] == DBNull.Value)
                {
                    return (isSuccess: false, isDuplicate: false, userId: 0, errorMessage: "Failed to retrieve generated User_id from tbl_user.");
                }

                long generatedUserId = Convert.ToInt64(userDt.Rows[0][0]);

                // ----------------------------------------------------
                // STEP 3: Insert deduplicated interests into MapUser_Interest
                // ----------------------------------------------------
                var distinctInterests = request.Interests?.Where(id => id > 0).Distinct().ToList() ?? new List<long>();

                if (distinctInterests.Count > 0)
                {
                    const string insertInterestSql = @"INSERT INTO dbo.MapUser_Interest (User_id, Interest_Id) VALUES (@User_id, @Interest_Id);";

                    foreach (var interestId in distinctInterests)
                    {
                        SqlParameter[] interestParams =
                        {
                            new SqlParameter("@User_id", SqlDbType.BigInt) { Value = generatedUserId },
                            new SqlParameter("@Interest_Id", SqlDbType.BigInt) { Value = interestId }
                        };

                        _dataAccess.GetDataTable(insertInterestSql, interestParams);
                    }
                }

                return (isSuccess: true, isDuplicate: false, userId: generatedUserId, errorMessage: null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SqlException during registration for Mobile: {Mobile}. Error Code: {Number}", request.MobileNo, ex.Number);
                _exceptionLogging.SendErrorToText(ex);

                // Handle unique index violation (error 2601 or 2627)
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    if (ex.Message.Contains("IX_tbl_user_Mobile", StringComparison.OrdinalIgnoreCase) ||
                        ex.Message.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                    {
                        return (isSuccess: false, isDuplicate: true, userId: 0, errorMessage: "Mobile number already registered.");
                    }
                }

                return (isSuccess: false, isDuplicate: false, userId: 0, errorMessage: "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for Mobile: {Mobile}", request.MobileNo);
                _exceptionLogging.SendErrorToText(ex);

                return (isSuccess: false, isDuplicate: false, userId: 0, errorMessage: "Unable to register user.");
            }
        }

        // ============================================================
        // LEGACY INSERT USER (Preserved for backwards compatibility)
        // ============================================================
        public DataTable InsertUser(string Username, string Password, string mobile)
        {
            SqlParameter _UserID = new SqlParameter("@UserID", mobile);
            SqlParameter _Username = new SqlParameter("@Username", Username);
            SqlParameter _Password = new SqlParameter("@Password", Password);
            SqlParameter _UserRole = new SqlParameter("@UserRole", "MOB");
            SqlParameter _Mobile = new SqlParameter("@Mobile", mobile);

            SqlParameter[] parameters = { _UserID, _Username, _Password, _UserRole, _Mobile };

            return _dataAccess.GetDataTableWithProc("user_insert", parameters);
        }

        // ============================================================
        // GET USER BY MOBILE WITH MULTIPLE INTERESTS (FROM MapUser_Interest)
        // ============================================================
        public async Task<UserData?> GetUserByMobileAsync(string mobileNo, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            const string userSql = @"SELECT TOP 1 User_id, Name, Mobile, DistCode, BlockCode, PanchayatCode, AreaType, VILLCODE, WARDCODE, Gender_code, Age_GroupID, Category_Id, Qualification_id, Occupation_id, Deviceid, Token, Entrydate
                                FROM dbo.tbl_user WHERE Mobile = @Mobile;";

            SqlParameter[] userParams =
            {
                new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = mobileNo?.Trim() ?? string.Empty }
            };

            DataTable dtUser = _dataAccess.GetDataTable(userSql, userParams);
            if (dtUser == null || dtUser.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = dtUser.Rows[0];
            var user = new UserData
            {
                UserId = Convert.ToInt64(row["User_id"]),
                Name = row["Name"] != DBNull.Value ? row["Name"].ToString() ?? string.Empty : string.Empty,
                MobileNo = row["Mobile"] != DBNull.Value ? row["Mobile"].ToString() ?? string.Empty : string.Empty,
                DistCode = row["DistCode"] != DBNull.Value ? Convert.ToInt32(row["DistCode"]) : null,
                BlockCode = row["BlockCode"] != DBNull.Value ? Convert.ToInt32(row["BlockCode"]) : null,
                PanchayatCode = row["PanchayatCode"] != DBNull.Value ? Convert.ToInt32(row["PanchayatCode"]) : null,
                AreaType = row["AreaType"] != DBNull.Value ? row["AreaType"].ToString() : null,
                VILLCode = row["VILLCODE"] != DBNull.Value ? row["VILLCODE"].ToString() : null,
                WARDCODE = row["WARDCODE"] != DBNull.Value ? row["WARDCODE"].ToString() : null,
                GenderCode = row["Gender_code"] != DBNull.Value ? Convert.ToInt32(row["Gender_code"]) : null,
                AgeGroupId = row["Age_GroupID"] != DBNull.Value ? Convert.ToInt32(row["Age_GroupID"]) : null,
                CategoryId = row["Category_Id"] != DBNull.Value ? Convert.ToInt32(row["Category_Id"]) : null,
                QualificationId = row["Qualification_id"] != DBNull.Value ? Convert.ToInt32(row["Qualification_id"]) : null,
                OccupationId = row["Occupation_id"] != DBNull.Value ? Convert.ToInt32(row["Occupation_id"]) : null,
                DeviceId = row["Deviceid"] != DBNull.Value ? row["Deviceid"].ToString() : null,
                Token = row["Token"] != DBNull.Value ? row["Token"].ToString() : null,
                EntryDate = row["Entrydate"] != DBNull.Value ? Convert.ToDateTime(row["Entrydate"]) : null,
                Interests = new List<long>(),
                UserInterests = new List<UserInterestData>()
            };

            // --------------------------------------------------------
            // Load multiple interests from MapUser_Interest
            // --------------------------------------------------------
            const string interestSql = @"SELECT mui.MapID, mui.Interest_Id, mi.interest_name 
                                         FROM dbo.MapUser_Interest mui 
                                         LEFT JOIN dbo.mst_interest mi ON mui.Interest_Id = mi.interest_id 
                                         WHERE mui.User_id = @UserId;";

            SqlParameter[] interestParams =
            {
                new SqlParameter("@UserId", SqlDbType.BigInt) { Value = user.UserId }
            };

            DataTable dtInterest = _dataAccess.GetDataTable(interestSql, interestParams);
            if (dtInterest != null && dtInterest.Rows.Count > 0)
            {
                foreach (DataRow interestRow in dtInterest.Rows)
                {
                    if (interestRow["Interest_Id"] != DBNull.Value)
                    {
                        long interestId = Convert.ToInt64(interestRow["Interest_Id"]);
                        user.Interests.Add(interestId);

                        user.UserInterests.Add(new UserInterestData
                        {
                            MapId = interestRow["MapID"] != DBNull.Value ? Convert.ToInt64(interestRow["MapID"]) : 0,
                            InterestId = interestId,
                            InterestName = interestRow["interest_name"] != DBNull.Value ? interestRow["interest_name"].ToString() : null
                        });
                    }
                }
            }

            return user;
        }

        // ============================================================
        // GET DEVICE ID BY MOBILE
        // ============================================================
        public async Task<string?> GetDeviceIdByMobileAsync(string mobileNo, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            try
            {
                const string sql = "SELECT TOP 1 Deviceid FROM dbo.tbl_user WHERE Mobile = @Mobile;";
                SqlParameter[] parameters =
                {
                    new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = mobileNo?.Trim() ?? string.Empty }
                };

                DataTable dt = _dataAccess.GetDataTable(sql, parameters);
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Deviceid"] != DBNull.Value)
                {
                    return dt.Rows[0]["Deviceid"].ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving DeviceId for mobile {Mobile}", mobileNo);
                _exceptionLogging.SendErrorToText(ex);
                throw;
            }
        }

        // ============================================================
        // UPDATE USER DEVICE ID IN tbl_user (WITHOUT CREATING DUPLICATE)
        // ============================================================
        public async Task<bool> UpdateUserDeviceIdAsync(string mobileNo, string deviceId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            try
            {
                string clientToken = _clientTokenService.GenerateToken();
                const string updateSql = @"UPDATE dbo.tbl_user SET Deviceid = @DeviceId, Token = @Token WHERE Mobile = @Mobile;";

                SqlParameter[] parameters =
                {
                    new SqlParameter("@DeviceId", SqlDbType.VarChar, 255) { Value = (object?)deviceId?.Trim() ?? DBNull.Value },
                    new SqlParameter("@Token", SqlDbType.VarChar, 255) { Value = (object?)clientToken ?? DBNull.Value },
                    new SqlParameter("@Mobile", SqlDbType.VarChar, 255) { Value = mobileNo?.Trim() ?? string.Empty }
                };

                int rowsAffected = _dataAccess.ExecuteSql(updateSql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DeviceId for mobile {Mobile}", mobileNo);
                _exceptionLogging.SendErrorToText(ex);
                throw;
            }
        }
    }
}
