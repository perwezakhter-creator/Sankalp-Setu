using System.Data;
using Microsoft.Data.SqlClient;
using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;

namespace SankalpAPI.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly clsDataAccess _dataAccess;
        private readonly ExceptionLogging _exceptionLogging;

        public DashboardRepository(clsDataAccess dataAccess, ExceptionLogging exceptionLogging)
        {
            _dataAccess = dataAccess;
            _exceptionLogging = exceptionLogging;
        }

        public Task<AreaWiseSummaryDto> GetAreaWiseSummaryAsync()
        {
            var summary = new AreaWiseSummaryDto();

            try
            {
                const string sql = @"
                    SELECT 
                        COUNT(1) AS TotalCount,
                        SUM(CASE WHEN AreaType = 'R' THEN 1 ELSE 0 END) AS RuralCount,
                        SUM(CASE WHEN AreaType = 'U' THEN 1 ELSE 0 END) AS UrbanCount,
                        SUM(CASE WHEN AreaType NOT IN ('R', 'U') OR AreaType IS NULL THEN 1 ELSE 0 END) AS UnspecifiedCount
                    FROM dbo.tbl_user;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    summary.Total = row["TotalCount"] != DBNull.Value ? Convert.ToInt32(row["TotalCount"]) : 0;
                    summary.RuralCount = row["RuralCount"] != DBNull.Value ? Convert.ToInt32(row["RuralCount"]) : 0;
                    summary.UrbanCount = row["UrbanCount"] != DBNull.Value ? Convert.ToInt32(row["UrbanCount"]) : 0;
                    summary.UnspecifiedCount = row["UnspecifiedCount"] != DBNull.Value ? Convert.ToInt32(row["UnspecifiedCount"]) : 0;

                    if (summary.Total > 0)
                    {
                        summary.RuralPercentage = Math.Round((double)summary.RuralCount / summary.Total * 100.0, 1);
                        summary.UrbanPercentage = Math.Round((double)summary.UrbanCount / summary.Total * 100.0, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(summary);
        }

        public Task<int> GetTotalBeneficiaryCountAsync()
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM dbo.tbl_beneficiary_details;";
                object? result = _dataAccess.ExecuteScalar(sql, null!);
                if (result != null && result != DBNull.Value)
                {
                    return Task.FromResult(Convert.ToInt32(result));
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(0);
        }

        public Task<(int districtCount, int blockCount, int panchayatCount)> GetGeographicCountsAsync()
        {
            int distCount = 0;
            int blockCount = 0;
            int panchayatCount = 0;

            try
            {
                const string sql = @"
                    SELECT 
                        (SELECT COUNT(1) FROM dbo.Districts) AS DistrictCount,
                        (SELECT COUNT(1) FROM dbo.Blocks) AS BlockCount,
                        (SELECT COUNT(1) FROM dbo.Panchayats) AS PanchayatCount;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    distCount = dt.Rows[0]["DistrictCount"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["DistrictCount"]) : 0;
                    blockCount = dt.Rows[0]["BlockCount"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["BlockCount"]) : 0;
                    panchayatCount = dt.Rows[0]["PanchayatCount"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["PanchayatCount"]) : 0;
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult((distCount, blockCount, panchayatCount));
        }

        public Task<List<DistrictWiseApplicantDto>> GetDistrictWiseApplicantsAsync()
        {
            var list = new List<DistrictWiseApplicantDto>();

            try
            {
                const string sql = @"
                    SELECT 
                        d.DistCode AS DistrictCode,
                        d.DistName AS DistrictName,
                        COALESCE(d.DistNameHN, '') AS DistrictNameHn,
                        COUNT(u.User_id) AS TotalRegistered,
                        SUM(CASE WHEN u.AreaType = 'R' THEN 1 ELSE 0 END) AS RuralCount,
                        SUM(CASE WHEN u.AreaType = 'U' THEN 1 ELSE 0 END) AS UrbanCount
                    FROM dbo.Districts d
                    LEFT JOIN dbo.tbl_user u ON TRY_CAST(d.DistCode AS INT) = u.DistCode
                    GROUP BY d.DistCode, d.DistName, d.DistNameHN
                    ORDER BY TotalRegistered DESC, d.DistName;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new DistrictWiseApplicantDto
                        {
                            DistrictCode = row["DistrictCode"]?.ToString() ?? string.Empty,
                            DistrictName = row["DistrictName"]?.ToString() ?? string.Empty,
                            DistrictNameHn = row["DistrictNameHn"]?.ToString() ?? string.Empty,
                            TotalRegistered = row["TotalRegistered"] != DBNull.Value ? Convert.ToInt32(row["TotalRegistered"]) : 0,
                            RuralCount = row["RuralCount"] != DBNull.Value ? Convert.ToInt32(row["RuralCount"]) : 0,
                            UrbanCount = row["UrbanCount"] != DBNull.Value ? Convert.ToInt32(row["UrbanCount"]) : 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(list);
        }

        public Task<List<BlockWiseApplicantDto>> GetBlockWiseApplicantsAsync(string? districtCode = null)
        {
            var list = new List<BlockWiseApplicantDto>();

            try
            {
                string sql = @"
                    SELECT 
                        b.DistCode AS DistrictCode,
                        b.BlockCode,
                        b.BlockName,
                        COALESCE(b.BlockNameHN, '') AS BlockNameHn,
                        COUNT(u.User_id) AS TotalRegistered,
                        SUM(CASE WHEN u.AreaType = 'R' THEN 1 ELSE 0 END) AS RuralCount,
                        SUM(CASE WHEN u.AreaType = 'U' THEN 1 ELSE 0 END) AS UrbanCount
                    FROM dbo.Blocks b
                    LEFT JOIN dbo.tbl_user u ON TRY_CAST(b.BlockCode AS INT) = u.BlockCode
                    WHERE (@DistrictCode IS NULL OR b.DistCode = @DistrictCode)
                    GROUP BY b.DistCode, b.BlockCode, b.BlockName, b.BlockNameHN
                    ORDER BY TotalRegistered DESC, b.BlockName;";

                SqlParameter[] parameters =
                {
                    new SqlParameter("@DistrictCode", string.IsNullOrWhiteSpace(districtCode) ? (object)DBNull.Value : districtCode.Trim())
                };

                DataTable dt = _dataAccess.GetDataTable(sql, parameters);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new BlockWiseApplicantDto
                        {
                            DistrictCode = row["DistrictCode"]?.ToString() ?? string.Empty,
                            BlockCode = row["BlockCode"]?.ToString() ?? string.Empty,
                            BlockName = row["BlockName"]?.ToString() ?? string.Empty,
                            BlockNameHn = row["BlockNameHn"]?.ToString() ?? string.Empty,
                            TotalRegistered = row["TotalRegistered"] != DBNull.Value ? Convert.ToInt32(row["TotalRegistered"]) : 0,
                            RuralCount = row["RuralCount"] != DBNull.Value ? Convert.ToInt32(row["RuralCount"]) : 0,
                            UrbanCount = row["UrbanCount"] != DBNull.Value ? Convert.ToInt32(row["UrbanCount"]) : 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(list);
        }

        public Task<List<DemographicItemDto>> GetGenderWiseApplicantsAsync()
        {
            var list = new List<DemographicItemDto>();

            try
            {
                const string sql = @"
                    SELECT 
                        COALESCE(g.gender_name, 'Not Specified') AS Name,
                        COUNT(u.User_id) AS Count
                    FROM dbo.tbl_user u
                    LEFT JOIN dbo.gender g ON u.Gender_code = g.gender_code
                    GROUP BY COALESCE(g.gender_name, 'Not Specified')
                    ORDER BY Count DESC;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null)
                {
                    int total = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        int cnt = row["Count"] != DBNull.Value ? Convert.ToInt32(row["Count"]) : 0;
                        total += cnt;
                        list.Add(new DemographicItemDto
                        {
                            Name = row["Name"]?.ToString() ?? "Not Specified",
                            Count = cnt
                        });
                    }

                    if (total > 0)
                    {
                        foreach (var item in list)
                        {
                            item.Percentage = Math.Round((double)item.Count / total * 100.0, 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(list);
        }

        public Task<List<DemographicItemDto>> GetCategoryWiseApplicantsAsync()
        {
            var list = new List<DemographicItemDto>();

            try
            {
                const string sql = @"
                    SELECT 
                        COALESCE(c.category_name, 'Not Specified') AS Name,
                        COUNT(u.User_id) AS Count
                    FROM dbo.tbl_user u
                    LEFT JOIN dbo.mst_category c ON u.Category_Id = c.category_id
                    GROUP BY COALESCE(c.category_name, 'Not Specified')
                    ORDER BY Count DESC;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null)
                {
                    int total = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        int cnt = row["Count"] != DBNull.Value ? Convert.ToInt32(row["Count"]) : 0;
                        total += cnt;
                        list.Add(new DemographicItemDto
                        {
                            Name = row["Name"]?.ToString() ?? "Not Specified",
                            Count = cnt
                        });
                    }

                    if (total > 0)
                    {
                        foreach (var item in list)
                        {
                            item.Percentage = Math.Round((double)item.Count / total * 100.0, 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(list);
        }

        public Task<List<RecentApplicantDto>> GetRecentApplicantsAsync(int count = 10)
        {
            var list = new List<RecentApplicantDto>();

            try
            {
                string sql = $@"
                    SELECT TOP {count} 
                        u.User_id,
                        u.Name,
                        u.Mobile,
                        COALESCE(d.DistName, 'N/A') AS DistrictName,
                        COALESCE(b.BlockName, 'N/A') AS BlockName,
                        CASE 
                            WHEN u.AreaType = 'R' THEN 'Rural' 
                            WHEN u.AreaType = 'U' THEN 'Urban' 
                            ELSE 'Not Specified' 
                        END AS AreaType,
                        u.Entrydate
                    FROM dbo.tbl_user u
                    LEFT JOIN dbo.Districts d ON u.DistCode = TRY_CAST(d.DistCode AS INT)
                    LEFT JOIN dbo.Blocks b ON u.BlockCode = TRY_CAST(b.BlockCode AS INT)
                    ORDER BY u.User_id DESC;";

                DataTable dt = _dataAccess.GetDataTable(sql);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string mobile = row["Mobile"]?.ToString() ?? string.Empty;
                        string maskedMobile = MaskMobile(mobile);

                        list.Add(new RecentApplicantDto
                        {
                            UserId = row["User_id"] != DBNull.Value ? Convert.ToInt64(row["User_id"]) : 0,
                            Name = row["Name"]?.ToString() ?? string.Empty,
                            MaskedMobile = maskedMobile,
                            DistrictName = row["DistrictName"]?.ToString() ?? string.Empty,
                            BlockName = row["BlockName"]?.ToString() ?? string.Empty,
                            AreaType = row["AreaType"]?.ToString() ?? string.Empty,
                            EntryDate = row["Entrydate"] != DBNull.Value ? Convert.ToDateTime(row["Entrydate"]) : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
            }

            return Task.FromResult(list);
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return string.Empty;
            string trimmed = mobile.Trim();
            if (trimmed.Length == 10)
            {
                return $"{trimmed.Substring(0, 2)}******{trimmed.Substring(8, 2)}";
            }
            return trimmed;
        }
    }
}
