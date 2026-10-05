using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.DTO;

namespace SankalpAPI.Services
{
    public class MasterService
    {
        private readonly IMasterDataService _masterDataService;

        public MasterService(IMasterDataService masterDataService)
        {
            _masterDataService = masterDataService;
        }

        public List<GenderList> GetGenderList()
        {
            return _masterDataService.GetGenderList();
        }
        public List<AgeGroupList> GetAgeGroupList()
        {
            return _masterDataService.GetAgeGroupList();
        }

        public List<DistrictList> GetDistrictList()
        {
            return _masterDataService.GetDistrictList();
        }

        public List<BlockList> GetBlockList(string DistCode)
        {
            return _masterDataService.GetBlockList(DistCode);
        }

        public List<PanchayatList> GetPanchayatList(string BlockCode)
        {
            return _masterDataService.GetPanchayatList(BlockCode);
        }
        public List<VillageList> GetVillageList(string PanchayatCode)
        {
            return _masterDataService.GetVillageList(PanchayatCode);
        }

        public List<WardList> GetWardList(string PanchayatCode)
        {
            return _masterDataService.GetWardList(PanchayatCode);
        }
        public List<CategoryList> GetCategoryList()
        {
            return _masterDataService.GetCategoryList();
        }

        public List<QualificationList> GetQualificationList()
        {
            return _masterDataService.GetQualificationList();
        }
        public List<OccupationList> GetOccupationList()
        {
            return _masterDataService.GetOccupationList();
        }
        public List<InterestList> GetInterestList()
        {
            return _masterDataService.GetInterestList();
        }
        public List<AppVersion> GetAppVersion()
        {
            return _masterDataService.GetAppVersion();
        }

        public List<GetPublicKey> GetPublicKey()
        {
            return _masterDataService.GetPublicKey();
        }
        public List<PortalDto> GetPortalList()
        {
            return _masterDataService.GetPortalList();
        }

       
    }
}
