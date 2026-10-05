using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SankalpAPI.DTO;
using SankalpAPI.Interface;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Retrieves the comprehensive dashboard metrics including:
        /// - Total Registered Applicants
        /// - Area-wise Distribution (Rural vs Urban count and percentages)
        /// - District-wise Total Registered Breakdown
        /// - Demographic Breakdowns (Gender, Category)
        /// - Recent Registered Applicants
        /// </summary>
        [HttpPost("Summary")]
        [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary([FromBody] DashboardSummaryRequest? request)
        {
            var response = await _dashboardService.GetDashboardSummaryAsync(request);
            if (!response.Status)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// GET endpoint for retrieving dashboard summary.
        /// </summary>
        [HttpGet("Summary")]
        [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummaryGet()
        {
            var response = await _dashboardService.GetDashboardSummaryAsync(null);
            if (!response.Status)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves block-wise applicant breakdown for a specific district.
        /// </summary>
        [HttpPost("BlockWise")]
        [ProducesResponseType(typeof(BlockWiseResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlockWise([FromBody] DashboardSummaryRequest? request)
        {
            var response = await _dashboardService.GetBlockWiseSummaryAsync(request?.DistrictCode);
            return Ok(response);
        }

        /// <summary>
        /// GET endpoint for block-wise applicant breakdown by district code.
        /// </summary>
        [HttpGet("BlockWise/{districtCode}")]
        [ProducesResponseType(typeof(BlockWiseResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlockWiseByCode(string districtCode)
        {
            var response = await _dashboardService.GetBlockWiseSummaryAsync(districtCode);
            return Ok(response);
        }
    }
}
