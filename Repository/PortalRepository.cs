using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SankalpAPI.Repository
{
    public class PortalRepository : IPortalRepository
    {
        private readonly clsDataAccess _dataAccess;

        public PortalRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task<List<PortalDto>> GetActivePortalsAsync()
        {
            var list = new List<PortalDto>();

            DataTable dt = _dataAccess.GetDataTableWithProc("GetPortalList", null);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new PortalDto
                {
                    PortalId = Convert.ToInt64(row["portal_id"]),
                    PortalName = row["portal_name"]?.ToString() ?? string.Empty,
                    PortalUrl = row["portal_url"] == DBNull.Value ? null : row["portal_url"]?.ToString(),
                    PortalImagePath = row["portal_image_path"] == DBNull.Value ? null : row["portal_image_path"]?.ToString(),
                    IsActive = row["is_active"] == DBNull.Value ? null : row["is_active"]?.ToString()
                });
            }

            return Task.FromResult(list);
        }

        public Task<PortalDto?> GetPortalByIdAsync(long portalId)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_portal_by_id", parameters);

            if (dt.Rows.Count == 0)
            {
                return Task.FromResult<PortalDto?>(null);
            }

            DataRow row = dt.Rows[0];
            var portal = new PortalDto
            {
                PortalId = Convert.ToInt64(row["portal_id"]),
                PortalName = row["portal_name"]?.ToString() ?? string.Empty,
                PortalUrl = row["portal_url"] == DBNull.Value ? null : row["portal_url"]?.ToString(),
                PortalImagePath = row["portal_image_path"] == DBNull.Value ? null : row["portal_image_path"]?.ToString(),
                IsActive = row["is_active"] == DBNull.Value ? null : row["is_active"]?.ToString()
            };

            return Task.FromResult<PortalDto?>(portal);
        }

        public Task<List<UserTypeDto>> GetUserTypeByIdAsync(long portalId)
        {
            var list = new List<UserTypeDto>();

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_userType_portal_by_id", parameters);            

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new UserTypeDto
                {
                    PortalId = Convert.ToInt64(row["portal_id"]),
                    UserType_id = row["User_Type_ID"]?.ToString() ?? string.Empty,
                    UserType_name = row["User_Type_Name"]?.ToString() ?? string.Empty,
                    UserType_name_hn = row["User_Type_Name_hn"]?.ToString() ?? string.Empty
                });
            }

            return Task.FromResult(list);
        }

        public Task<PortalUserMappingDto?> GetPortalAndUserMappingAsync(long portalId, string? mobile)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@p_portal_id", portalId),
                new SqlParameter("@p_mobile", string.IsNullOrWhiteSpace(mobile) ? DBNull.Value : mobile.Trim())
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_portal_and_user_mapping", parameters);

            if (dt.Rows.Count == 0)
            {
                return Task.FromResult<PortalUserMappingDto?>(null);
            }

            DataRow row = dt.Rows[0];
            var dto = new PortalUserMappingDto
            {
                PortalId = Convert.ToInt64(row["portal_id"]),
                PortalName = row["portal_name"]?.ToString() ?? string.Empty,
                PortalUrl = row["portal_url"] == DBNull.Value ? null : row["portal_url"]?.ToString(),
                IsActive = row["is_active"] == DBNull.Value ? null : row["is_active"]?.ToString(),
                UserPortalId = row["user_portal_id"] == DBNull.Value ? null : Convert.ToInt64(row["user_portal_id"]),
                Mobile = row["mobile"] == DBNull.Value ? null : row["mobile"]?.ToString(),
                UserId = row["userid"] == DBNull.Value ? null : row["userid"]?.ToString(),
                SecretKey = row["secretkey"] == DBNull.Value ? null : row["secretkey"]?.ToString(),
                Token = row["token"] == DBNull.Value ? null : row["token"]?.ToString()
            };

            return Task.FromResult<PortalUserMappingDto?>(dto);
        }
    }
}
