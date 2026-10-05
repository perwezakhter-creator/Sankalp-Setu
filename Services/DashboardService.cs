using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Security;

namespace SankalpAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ExceptionLogging _exceptionLogging;

        public DashboardService(IDashboardRepository dashboardRepository, ExceptionLogging exceptionLogging)
        {
            _dashboardRepository = dashboardRepository;
            _exceptionLogging = exceptionLogging;
        }

        public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardSummaryRequest? request = null)
        {
            try
            {
                var areaWise = await _dashboardRepository.GetAreaWiseSummaryAsync();
                var beneficiaryCount = await _dashboardRepository.GetTotalBeneficiaryCountAsync();
                var (districtCount, blockCount, panchayatCount) = await _dashboardRepository.GetGeographicCountsAsync();
                var districtWise = await _dashboardRepository.GetDistrictWiseApplicantsAsync();
                var genderWise = await _dashboardRepository.GetGenderWiseApplicantsAsync();
                var categoryWise = await _dashboardRepository.GetCategoryWiseApplicantsAsync();
                var recentApplicants = await _dashboardRepository.GetRecentApplicantsAsync(10);

                var data = new DashboardSummaryData
                {
                    TotalRegisteredApplicants = areaWise.Total,
                    TotalBeneficiaries = beneficiaryCount,
                    TotalDistricts = districtCount,
                    TotalBlocks = blockCount,
                    TotalPanchayats = panchayatCount,
                    AreaWise = areaWise,
                    DistrictWise = districtWise,
                    GenderWise = genderWise,
                    CategoryWise = categoryWise,
                    RecentApplicants = recentApplicants
                };

                // Decrypt client key if passed, and populate encrypted compatibility fields
                if (!string.IsNullOrWhiteSpace(request?.ClientKey))
                {
                    try
                    {
                        string clientKey = RSA_Enc_Dec.Decryption(request.ClientKey);
                        if (!string.IsNullOrWhiteSpace(clientKey))
                        {
                            data.DistrictCount = Aes256CbcEncrypterApp.Encrypt(districtCount.ToString(), clientKey);
                            data.BlockCount = Aes256CbcEncrypterApp.Encrypt(blockCount.ToString(), clientKey);
                            data.PanchayatCount = Aes256CbcEncrypterApp.Encrypt(panchayatCount.ToString(), clientKey);
                            data.AwayabCount = Aes256CbcEncrypterApp.Encrypt(areaWise.Total.ToString(), clientKey);
                            data.DepartmentCount = Aes256CbcEncrypterApp.Encrypt(beneficiaryCount.ToString(), clientKey);
                            data.SchemeCount = Aes256CbcEncrypterApp.Encrypt(areaWise.RuralCount.ToString(), clientKey);
                        }
                    }
                    catch (Exception ex)
                    {
                        _exceptionLogging.SendErrorToText(ex);
                    }
                }

                return new DashboardSummaryResponse
                {
                    Status = true,
                    Message = "Dashboard summary retrieved successfully.",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return new DashboardSummaryResponse
                {
                    Status = false,
                    Message = "Unable to retrieve dashboard summary.",
                    Data = null
                };
            }
        }

        public async Task<BlockWiseResponse> GetBlockWiseSummaryAsync(string? districtCode)
        {
            try
            {
                var blocks = await _dashboardRepository.GetBlockWiseApplicantsAsync(districtCode);

                return new BlockWiseResponse
                {
                    Status = true,
                    Message = "Block-wise summary retrieved successfully.",
                    DistrictCode = districtCode ?? string.Empty,
                    Data = blocks
                };
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);
                return new BlockWiseResponse
                {
                    Status = false,
                    Message = "Unable to retrieve block-wise summary.",
                    DistrictCode = districtCode ?? string.Empty,
                    Data = new List<BlockWiseApplicantDto>()
                };
            }
        }
    }
}
