using Microsoft.Data.SqlClient;
using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using System.Data;
using System.Security.Cryptography.X509Certificates;

namespace SankalpAPI.Repository
{
    public class MasterRepository : IMasterDataService
    {
        private readonly clsDataAccess _dataAccess;

        public MasterRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        // ============================================================
        // GET Gender List
        // ============================================================

        public List<GenderList> GetGenderList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetGenderList", null);

            List<GenderList> glist = new();

            foreach (DataRow row in dt.Rows)
            {
                glist.Add(new GenderList
                {
                    gender_code = row["gender_code"]?.ToString(),
                    gender_name = row["gender_name"]?.ToString(),
                    gender_name_hn = row["gender_name_hn"]?.ToString()
                });
            }

            return glist;
        }

        // ============================================================
        // GET Age Group List
        // ============================================================

        public List<AgeGroupList> GetAgeGroupList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetAgeGroupList", null);

            List<AgeGroupList> aglist = new();

            foreach (DataRow row in dt.Rows)
            {
                aglist.Add(new AgeGroupList
                {
                    id = row["id"]?.ToString(),
                    age_group_name = row["age_group_name"]?.ToString(),
                    age_group_name_hn = row["age_group_name_hn"]?.ToString()
                });
            }

            return aglist;
        }

        // ============================================================
        // GET District List
        // ============================================================

        public List<DistrictList> GetDistrictList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetDistrictList", null);

            List<DistrictList> dlist = new();

            foreach (DataRow row in dt.Rows)
            {
                dlist.Add(new DistrictList
                {
                    District_Code = row["District_Code"]?.ToString(),
                    District_Name = row["District_Name"]?.ToString(),
                    District_Name_Hn = row["District_Name_Hn"]?.ToString()
                });
            }

            return dlist;
        }

        // ============================================================
        // GET Block List
        // ============================================================

        public List<BlockList> GetBlockList(string DistCode)
        {
            SqlParameter _DistCode = new SqlParameter("@District_Code", DistCode);
            SqlParameter[] parameters = { _DistCode };
            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetBlockList", parameters);

            List<BlockList> blist = new();

            foreach (DataRow row in dt.Rows)
            {
                blist.Add(new BlockList
                {
                    BlockCode = row["BlockCode"]?.ToString(),
                    BlockName = row["BlockName"]?.ToString(),
                    BlockNameHN = row["BlockNameHN"]?.ToString()
                });
            }

            return blist;
        }


        // ============================================================
        // GET Panchayat List
        // ============================================================

        public List<PanchayatList> GetPanchayatList(string BlockCode)
        {
            SqlParameter _BlockCode = new SqlParameter("@BlockCode", BlockCode);
            SqlParameter[] parameters = { _BlockCode };
            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetPanchayatList", parameters);

            List<PanchayatList> plist = new();

            foreach (DataRow row in dt.Rows)
            {
                plist.Add(new PanchayatList
                {
                    PanchayatCode = row["PanchayatCode"]?.ToString(),
                    PanchayatName = row["PanchayatName"]?.ToString(),
                    PanchayatNameHnd = row["PanchayatNameHnd"]?.ToString(),
                    AreaType = row["AreaType"]?.ToString()
                });
            }

            return plist;
        }

        // ============================================================
        // GET Village List
        // ============================================================

        public List<VillageList> GetVillageList(string PanchayatCode)
        {
            SqlParameter _PanchayatCode = new SqlParameter("@PanchayatCode", PanchayatCode);
            SqlParameter[] parameters = { _PanchayatCode };
            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetVillageList", parameters);

            List<VillageList> vlist = new();

            foreach (DataRow row in dt.Rows)
            {
                vlist.Add(new VillageList
                {
                    VILLCODE = row["VILLCODE"]?.ToString(),
                    VILLNAME = row["VILLNAME"]?.ToString()
                });
            }

            return vlist;
        }

        // ============================================================
        // GET Ward List
        // ============================================================
        public List<WardList> GetWardList(string PanchayatCode)
        {
            SqlParameter _PanchayatCode = new SqlParameter("@PanchayatCode", PanchayatCode);
            SqlParameter[] parameters = { _PanchayatCode };
            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetWardList", parameters);

            List<WardList> wlist = new();

            foreach (DataRow row in dt.Rows)
            {
                wlist.Add(new WardList
                {
                    WARDCODE = row["WARDCODE"]?.ToString(),
                    WARDNAME = row["WARDNAME"]?.ToString()
                });
            }

            return wlist;
        }



        // ============================================================
        // GET Category List
        // ============================================================

        public List<CategoryList> GetCategoryList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetCategoryList", null);

            List<CategoryList> clist = new();

            foreach (DataRow row in dt.Rows)
            {
                clist.Add(new CategoryList
                {
                    category_id = row["category_id"]?.ToString(),
                    category_name = row["category_name"]?.ToString(),
                    category_name_hn = row["category_name_hn"]?.ToString(),
                    is_active = row["is_active"]?.ToString()
                });
            }

            return clist;
        }

        // ============================================================
        // GET Qualification List
        // ============================================================

        public List<QualificationList> GetQualificationList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetQualificationList", null);

            List<QualificationList> qlist = new();

            foreach (DataRow row in dt.Rows)
            {
                qlist.Add(new QualificationList
                {
                    qualification_id = row["qualification_id"]?.ToString(),
                    qualification_name = row["qualification_name"]?.ToString(),
                    qualification_name_hn = row["qualification_name_hn"]?.ToString(),
                    is_active = row["is_active"]?.ToString()
                });
            }

            return qlist;
        }

        // ============================================================
        // GET Occupation List
        // ============================================================

        public List<OccupationList> GetOccupationList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetOccupationList", null);

            List<OccupationList> olist = new();

            foreach (DataRow row in dt.Rows)
            {
                olist.Add(new OccupationList
                {
                    occupation_id = row["occupation_id"]?.ToString(),
                    occupation_name = row["occupation_name"]?.ToString(),
                    occupation_name_hn = row["occupation_name_hn"]?.ToString(),
                    is_active = row["is_active"]?.ToString()
                });
            }

            return olist;
        }

        // ============================================================
        // GET Interest List
        // ============================================================

        public List<InterestList> GetInterestList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetInterestList", null);

            List<InterestList> ilist = new();

            foreach (DataRow row in dt.Rows)
            {
                ilist.Add(new InterestList
                {
                    interest_id = row["interest_id"]?.ToString(),
                    interest_name = row["interest_name"]?.ToString(),
                    interest_name_hn = row["interest_name_hn"]?.ToString(),
                    is_active = row["is_active"]?.ToString()
                });
            }

            return ilist;
        }

        // ============================================================
        // GET App Version
        // ============================================================

        public List<AppVersion> GetAppVersion()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetAppDetails", null);

            List<AppVersion> appVersions = new();

            foreach (DataRow row in dt.Rows)
            {
                appVersions.Add(new AppVersion
                {
                    SlNo = row["SlNo"]?.ToString(),
                    Priority = row["Priority"]?.ToString(),
                    AdminMsg = row["AdminMsg"]?.ToString(),
                    AdminTitle = row["AdminTitle"]?.ToString(),
                    UpdateMsg = row["UpdateMsg"]?.ToString(),
                    UpdateTitle = row["UpdateTitle"]?.ToString(),
                    Role = row["Role"]?.ToString(),
                    Version = row["ver"]?.ToString(),
                    AppUrl = row["AppUrl"]?.ToString()
                });
            }

            return appVersions;
        }

        // ============================================================
        // GET PublicKey
        // ============================================================

        public List<GetPublicKey> GetPublicKey()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetPublicKey", null);

            List<GetPublicKey> publicKeys = new();

            foreach (DataRow row in dt.Rows)
            {
                publicKeys.Add(new GetPublicKey 
                {
                    PublicKey = row["PublicKey"]?.ToString()
                });
            }

            return publicKeys;
        }


        // ============================================================
        // GET ALL Portal List
        // ============================================================
        public List<PortalDto> GetPortalList()
        {

            DataTable dt = _dataAccess.GetDataTableWithProc("dbo.GetPortalList", null);

            List<PortalDto> plist = new();

            foreach (DataRow row in dt.Rows)
            {
                plist.Add(new PortalDto
                {
                    PortalId = Convert.ToInt64(row["portal_id"]?.ToString()),
                    PortalName = row["portal_name"]?.ToString(),
                    PortalImagePath = row["portal_image_path"]?.ToString(),
                    PortalUrl = row["portal_url"]?.ToString(),
                });
            }

            return plist;
        }

      
    }
}
