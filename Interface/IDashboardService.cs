using SankalpAPI.DTO;

namespace SankalpAPI.Interface
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardSummaryRequest? request = null);
        Task<BlockWiseResponse> GetBlockWiseSummaryAsync(string? districtCode);
    }
}
