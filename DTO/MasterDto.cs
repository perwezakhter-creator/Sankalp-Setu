namespace SankalpAPI.DTO
{
    public class MasterDto
    {
        public string ClientKey { get; set; } = string.Empty;
    }

    public class GenderList
    {
        public string? gender_code { get; set; }
        public string? gender_name { get; set; }
        public string? gender_name_hn { get; set; }
    }

    public class AgeGroupList
    {
        public string? id { get; set; }
        public string? age_group_name { get; set; }
        public string? age_group_name_hn { get; set; }
    }

    public class DistrictList
    {
        public string? District_Code { get; set; }
        public string? District_Name { get; set; }
        public string? District_Name_Hn { get; set; }
    }

    public class BlockList
    {
        public string? BlockCode { get; set; }
        public string? BlockName { get; set; }
        public string? BlockNameHN { get; set; }
    }

    public class PanchayatList
    {
        public string? PanchayatCode { get; set; }
        public string? PanchayatName { get; set; }
        public string? PanchayatNameHnd { get; set; }
        public string? AreaType { get; set; }
    }

    public class VillageList
    {
        public string? VILLCODE { get; set; }
        public string? VILLNAME { get; set; }       
    }

    public class WardList
    {
        public string? WARDCODE { get; set; }
        public string? WARDNAME { get; set; }
    }

    public class CategoryList
    {
        public string? category_id { get; set; }
        public string? category_name { get; set; }
        public string? category_name_hn { get; set; }
        public string? is_active { get; set; }
    }

    public class QualificationList
    {
        public string? qualification_id { get; set; }
        public string? qualification_name { get; set; }
        public string? qualification_name_hn { get; set; }
        public string? is_active { get; set; }
    }
    public class OccupationList
    {
        public string? occupation_id { get; set; }
        public string? occupation_name { get; set; }
        public string? occupation_name_hn { get; set; }
        public string? is_active { get; set; }
    }

    public class InterestList
    {
        public string? interest_id { get; set; }
        public string? interest_name { get; set; }
        public string? interest_name_hn { get; set; }
        public string? is_active { get; set; }
    }

    public class AppVersion
    {
        public string? SlNo { get; set; }
        public string? Priority { get; set; }
        public string? AdminMsg { get; set; }
        public string? AdminTitle { get; set; }
        public string? UpdateMsg { get; set; }
        public string? UpdateTitle { get; set; }
        public string? Role { get; set; }
        public string? Version { get; set; }
        public string? AppUrl { get; set; }
    }

    public class GetPublicKey
    {
        public string? PublicKey { get; set; }
    }


}
