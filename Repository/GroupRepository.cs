using System.Data;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Repository
{
    public class GroupRepository : IGroupRepository
    {
        private readonly clsDataAccess _dataAccess;

        public GroupRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task<List<GroupDto>> GetGroupsAsync()
        {
            const string query = @"
                SELECT portal_id, portal_name, portal_url, portal_image_path, is_active
                FROM dbo.mst_portal
                ORDER BY portal_name, portal_id;";

            DataTable table = _dataAccess.GetDataTable(query, Array.Empty<SqlParameter>());
            var groups = new List<GroupDto>();

            foreach (DataRow row in table.Rows)
            {
                groups.Add(MapGroup(row));
            }

            return Task.FromResult(groups);
        }

        public Task<List<SubGroupDto>> GetSubGroupsAsync()
        {
            const string query = @"
                SELECT ut.ID,
                       ut.Portal_ID,
                       p.portal_name,
                       ut.User_Type_ID,
                       ut.User_Type_Name,
                       ut.User_Type_Name_hn,
                       ut.IsActive
                FROM dbo.UserType ut
                INNER JOIN dbo.mst_portal p ON p.portal_id = ut.Portal_ID
                ORDER BY p.portal_name, TRY_CONVERT(bigint, ut.User_Type_ID), ut.ID;";

            DataTable table = _dataAccess.GetDataTable(query, Array.Empty<SqlParameter>());
            var subGroups = new List<SubGroupDto>();

            foreach (DataRow row in table.Rows)
            {
                subGroups.Add(MapSubGroup(row));
            }

            return Task.FromResult(subGroups);
        }

        public Task<bool> GroupNameExistsAsync(string groupName)
        {
            const string query = @"
                SELECT COUNT(1)
                FROM dbo.mst_portal
                WHERE LOWER(LTRIM(RTRIM(portal_name))) = LOWER(@portal_name);";

            object? result = _dataAccess.ExecuteScalar(query, new[]
            {
                TextParameter("@portal_name", groupName, 255)
            });

            return Task.FromResult(Convert.ToInt32(result ?? 0) > 0);
        }

        public Task<bool> GroupExistsAsync(long portalId)
        {
            const string query = "SELECT COUNT(1) FROM dbo.mst_portal WHERE portal_id = @portal_id;";
            object? result = _dataAccess.ExecuteScalar(query, new[]
            {
                new SqlParameter("@portal_id", SqlDbType.BigInt) { Value = portalId }
            });

            return Task.FromResult(Convert.ToInt32(result ?? 0) > 0);
        }

        public Task<bool> SubGroupNameExistsAsync(long portalId, string subGroupName)
        {
            const string query = @"
                SELECT COUNT(1)
                FROM dbo.UserType
                WHERE Portal_ID = @portal_id
                  AND LOWER(LTRIM(RTRIM(User_Type_Name))) = LOWER(@user_type_name);";

            object? result = _dataAccess.ExecuteScalar(query, new[]
            {
                new SqlParameter("@portal_id", SqlDbType.BigInt) { Value = portalId },
                TextParameter("@user_type_name", subGroupName, 255)
            });

            return Task.FromResult(Convert.ToInt32(result ?? 0) > 0);
        }

        public Task<GroupDto?> CreateGroupAsync(CreateGroupRequest request)
        {
            const string query = @"
                INSERT INTO dbo.mst_portal (portal_name, portal_url, portal_image_path, is_active)
                OUTPUT INSERTED.portal_id,
                       INSERTED.portal_name,
                       INSERTED.portal_url,
                       INSERTED.portal_image_path,
                       INSERTED.is_active
                VALUES (@portal_name, @portal_url, @portal_image_path, @is_active);";

            DataTable table = _dataAccess.GetDataTable(query, new[]
            {
                TextParameter("@portal_name", request.PortalName, 255),
                TextParameter("@portal_url", request.PortalUrl, 2000),
                TextParameter("@portal_image_path", request.PortalImagePath, 1000),
                new SqlParameter("@is_active", SqlDbType.Char, 1) { Value = request.IsActive ?? "Y" }
            });

            return Task.FromResult(table.Rows.Count == 0 ? null : MapGroup(table.Rows[0]));
        }

        public Task<SubGroupDto?> CreateSubGroupAsync(CreateSubGroupRequest request)
        {
            // User_Type_ID is scoped to the parent Portal_ID.  The update lock makes the
            // next sequence value safe when two administrators create a subgroup together.
            const string query = @"
                DECLARE @next_user_type_id bigint;
                DECLARE @created TABLE (
                    ID bigint,
                    Portal_ID bigint,
                    User_Type_ID bigint,
                    User_Type_Name nvarchar(255),
                    User_Type_Name_hn nvarchar(255),
                    IsActive char(1)
                );

                SELECT @next_user_type_id = ISNULL(MAX(TRY_CONVERT(bigint, User_Type_ID)), 0) + 1
                FROM dbo.UserType WITH (UPDLOCK, HOLDLOCK)
                WHERE Portal_ID = @portal_id;

                INSERT INTO dbo.UserType (Portal_ID, User_Type_ID, User_Type_Name, User_Type_Name_hn, IsActive)
                OUTPUT INSERTED.ID,
                       INSERTED.Portal_ID,
                       INSERTED.User_Type_ID,
                       INSERTED.User_Type_Name,
                       INSERTED.User_Type_Name_hn,
                       INSERTED.IsActive
                INTO @created
                VALUES (@portal_id, @next_user_type_id, @user_type_name, @user_type_name_hn, @is_active);

                SELECT c.ID,
                       c.Portal_ID,
                       p.portal_name,
                       c.User_Type_ID,
                       c.User_Type_Name,
                       c.User_Type_Name_hn,
                       c.IsActive
                FROM @created c
                INNER JOIN dbo.mst_portal p ON p.portal_id = c.Portal_ID;";

            DataTable table = _dataAccess.GetDataTable(query, new[]
            {
                new SqlParameter("@portal_id", SqlDbType.BigInt) { Value = request.PortalId },
                TextParameter("@user_type_name", request.UserTypeName, 255),
                TextParameter("@user_type_name_hn", request.UserTypeNameHn, 255),
                new SqlParameter("@is_active", SqlDbType.Char, 1) { Value = request.IsActive ?? "Y" }
            });

            return Task.FromResult(table.Rows.Count == 0 ? null : MapSubGroup(table.Rows[0]));
        }

        private static SqlParameter TextParameter(string name, string? value, int length)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, length)
            {
                Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim()
            };
        }

        private static GroupDto MapGroup(DataRow row)
        {
            return new GroupDto
            {
                PortalId = Convert.ToInt64(row["portal_id"]),
                PortalName = row["portal_name"]?.ToString() ?? string.Empty,
                PortalUrl = StringValue(row, "portal_url"),
                PortalImagePath = StringValue(row, "portal_image_path"),
                IsActive = StringValue(row, "is_active") ?? "Y"
            };
        }

        private static SubGroupDto MapSubGroup(DataRow row)
        {
            return new SubGroupDto
            {
                Id = Convert.ToInt64(row["ID"]),
                PortalId = Convert.ToInt64(row["Portal_ID"]),
                PortalName = row["portal_name"]?.ToString() ?? string.Empty,
                UserTypeId = Convert.ToInt64(row["User_Type_ID"]),
                UserTypeName = row["User_Type_Name"]?.ToString() ?? string.Empty,
                UserTypeNameHn = StringValue(row, "User_Type_Name_hn"),
                IsActive = StringValue(row, "IsActive") ?? "Y"
            };
        }

        private static string? StringValue(DataRow row, string columnName)
        {
            return row[columnName] == DBNull.Value ? null : row[columnName]?.ToString();
        }
    }
}
