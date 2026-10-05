using SankalpAPI.DTO;

namespace SankalpAPI.Interface
{
    public interface IDashboardRepository
    {
        Task<AreaWiseSummaryDto> GetAreaWiseSummaryAsync();
        Task<int> GetTotalBeneficiaryCountAsync();
        Task<(int districtCount, int blockCount, int panchayatCount)> GetGeographicCountsAsync();
        Task<List<DistrictWiseApplicantDto>> GetDistrictWiseApplicantsAsync();
        Task<List<BlockWiseApplicantDto>> GetBlockWiseApplicantsAsync(string? districtCode = null);
        Task<List<DemographicItemDto>> GetGenderWiseApplicantsAsync();
        Task<List<DemographicItemDto>> GetCategoryWiseApplicantsAsync();
        Task<List<RecentApplicantDto>> GetRecentApplicantsAsync(int count = 10);
    }
}
