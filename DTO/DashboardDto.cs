using System.Text.Json.Serialization;

namespace SankalpAPI.DTO
{
    public class DashboardSummaryRequest
    {
        [JsonPropertyName("clientKey")]
        public string? ClientKey { get; set; }

        [JsonPropertyName("districtCode")]
        public string? DistrictCode { get; set; }
    }

    public class AreaWiseSummaryDto
    {
        [JsonPropertyName("ruralCount")]
        public int RuralCount { get; set; }

        [JsonPropertyName("urbanCount")]
        public int UrbanCount { get; set; }

        [JsonPropertyName("unspecifiedCount")]
        public int UnspecifiedCount { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("ruralPercentage")]
        public double RuralPercentage { get; set; }

        [JsonPropertyName("urbanPercentage")]
        public double UrbanPercentage { get; set; }
    }

    public class DistrictWiseApplicantDto
    {
        [JsonPropertyName("districtCode")]
        public string DistrictCode { get; set; } = string.Empty;

        [JsonPropertyName("districtName")]
        public string DistrictName { get; set; } = string.Empty;

        [JsonPropertyName("districtNameHn")]
        public string DistrictNameHn { get; set; } = string.Empty;

        [JsonPropertyName("totalRegistered")]
        public int TotalRegistered { get; set; }

        [JsonPropertyName("ruralCount")]
        public int RuralCount { get; set; }

        [JsonPropertyName("urbanCount")]
        public int UrbanCount { get; set; }
    }

    public class BlockWiseApplicantDto
    {
        [JsonPropertyName("districtCode")]
        public string DistrictCode { get; set; } = string.Empty;

        [JsonPropertyName("blockCode")]
        public string BlockCode { get; set; } = string.Empty;

        [JsonPropertyName("blockName")]
        public string BlockName { get; set; } = string.Empty;

        [JsonPropertyName("blockNameHn")]
        public string BlockNameHn { get; set; } = string.Empty;

        [JsonPropertyName("totalRegistered")]
        public int TotalRegistered { get; set; }

        [JsonPropertyName("ruralCount")]
        public int RuralCount { get; set; }

        [JsonPropertyName("urbanCount")]
        public int UrbanCount { get; set; }
    }

    public class DemographicItemDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }
    }

    public class RecentApplicantDto
    {
        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("maskedMobile")]
        public string MaskedMobile { get; set; } = string.Empty;

        [JsonPropertyName("districtName")]
        public string DistrictName { get; set; } = string.Empty;

        [JsonPropertyName("blockName")]
        public string BlockName { get; set; } = string.Empty;

        [JsonPropertyName("areaType")]
        public string AreaType { get; set; } = string.Empty;

        [JsonPropertyName("entryDate")]
        public DateTime? EntryDate { get; set; }
    }

    public class DashboardSummaryData
    {
        [JsonPropertyName("totalRegisteredApplicants")]
        public int TotalRegisteredApplicants { get; set; }

        [JsonPropertyName("totalBeneficiaries")]
        public int TotalBeneficiaries { get; set; }

        [JsonPropertyName("totalDistricts")]
        public int TotalDistricts { get; set; }

        [JsonPropertyName("totalBlocks")]
        public int TotalBlocks { get; set; }

        [JsonPropertyName("totalPanchayats")]
        public int TotalPanchayats { get; set; }

        [JsonPropertyName("areaWise")]
        public AreaWiseSummaryDto AreaWise { get; set; } = new();

        [JsonPropertyName("districtWise")]
        public List<DistrictWiseApplicantDto> DistrictWise { get; set; } = new();

        [JsonPropertyName("genderWise")]
        public List<DemographicItemDto> GenderWise { get; set; } = new();

        [JsonPropertyName("categoryWise")]
        public List<DemographicItemDto> CategoryWise { get; set; } = new();

        [JsonPropertyName("recentApplicants")]
        public List<RecentApplicantDto> RecentApplicants { get; set; } = new();

        // Legacy compatibility fields (encrypted string values if ClientKey was provided)
        [JsonPropertyName("districtCount")]
        public string? DistrictCount { get; set; }

        [JsonPropertyName("blockCount")]
        public string? BlockCount { get; set; }

        [JsonPropertyName("panchayatCount")]
        public string? PanchayatCount { get; set; }

        [JsonPropertyName("awayabCount")]
        public string? AwayabCount { get; set; }

        [JsonPropertyName("departmentCount")]
        public string? DepartmentCount { get; set; }

        [JsonPropertyName("schemeCount")]
        public string? SchemeCount { get; set; }
    }

    public class DashboardSummaryResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public DashboardSummaryData? Data { get; set; }
    }

    public class BlockWiseResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("districtCode")]
        public string DistrictCode { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<BlockWiseApplicantDto> Data { get; set; } = new();
    }
}
