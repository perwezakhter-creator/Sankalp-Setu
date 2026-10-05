using SankalpAPI.DTO;
using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IMasterDataService
    {
       
        List<GenderList> GetGenderList();
        List<AgeGroupList> GetAgeGroupList();
        List<DistrictList> GetDistrictList();
        List<BlockList> GetBlockList(string DistCode);
        List<PanchayatList> GetPanchayatList(string BlockCode);
        List<VillageList> GetVillageList(string PanchayatCode);
        List<WardList> GetWardList(string PanchayatCode);
        List<CategoryList> GetCategoryList();
        List<QualificationList> GetQualificationList();
        List<OccupationList> GetOccupationList();
        List<InterestList> GetInterestList();
        List<AppVersion> GetAppVersion();
        List<GetPublicKey> GetPublicKey();
        List<PortalDto> GetPortalList();
    }
}
