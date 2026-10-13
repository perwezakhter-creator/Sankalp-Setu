using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [AllowAnonymous]
    public class ApplicantController : ControllerBase
    {
        private readonly IApplicantService _applicantService;

        public ApplicantController(IApplicantService applicantService)
        {
            _applicantService = applicantService;
        }

        /// <summary>
        /// Accesses an external portal API (eLabharthi / Sankalp) passing portal_id (mapped as portType to external API),
        /// mobile_no, deviceid, session in JSON format, saves the applicant details into the database, and returns the beneficiary records.
        /// If the portal's URL is unavailable/unconfigured (e.g. portal_id = 5), initiates OTP-based applicant registration via SMS.
        /// </summary>
        /// <remarks>
        /// Sample Request (Portal URL available):
        /// 
        ///     POST /api/Applicant/GetApplicantDetails
        ///     {
        ///       "portal_id": 1,
        ///       "mobile_no": "9876543210",
        ///       "deviceid": "DEVICE123456",
        ///       "session": "2024-2025"
        ///     }
        /// 
        /// Sample Request (Portal URL unavailable - initiates OTP-based registration):
        /// 
        ///     POST /api/Applicant/GetApplicantDetails
        ///     {
        ///       "portal_id": 5,
        ///       "mobile_no": "9507637005",
        ///       "deviceid": "DEV123",
        ///       "session": "2026",
        ///       "UserType": "2"
        ///     }
        /// </remarks>
        [HttpPost("GetApplicantDetails")]
        [ProducesResponseType(typeof(ApplicantListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> GetApplicantDetails([FromBody] ApplicantRequest request, CancellationToken cancellationToken)
        {
            var response = await _applicantService.GetApplicantDetailsAsync(request, cancellationToken);

            if (response.StatusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest(response);
            }

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                return NotFound(response);
            }

            if (response.StatusCode == StatusCodes.Status502BadGateway)
            {
                return StatusCode(StatusCodes.Status502BadGateway, response);
            }

            if (response.StatusCode == StatusCodes.Status504GatewayTimeout)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, response);
            }

            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Verifies OTP via the external portal API (when portal URL is configured) or via server-side OTP verification
        /// (when portal URL is unavailable, inserting the applicant mapping into tbl_map_user_portal).
        /// </summary>
        /// <remarks>
        /// Sample Request (External Portal OTP Verification):
        /// 
        ///     POST /api/Applicant/VerifyOtp
        ///     {
        ///       "a_Id": [73, 74],
        ///       "mobileNo": "9097372728",
        ///       "otp": "558861",
        ///       "deviceId": "DEV123",
        ///       "session": "2025"
        ///     }
        /// 
        /// Sample Request (Internal OTP-based Applicant Registration):
        /// 
        ///     POST /api/Applicant/VerifyOtp
        ///     {
        ///       "portal_id": 5,
        ///       "mobile_no": "9507637005",
        ///       "otp": "123456",
        ///       "deviceid": "DEV123",
        ///       "session": "2026",
        ///       "UserType": "2"
        ///     }
        /// </remarks>
        [HttpPost("VerifyOtp")]
        [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status504GatewayTimeout)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
        {
            var response = await _applicantService.VerifyOtpAsync(request, cancellationToken);

            if (response.StatusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest(response);
            }

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                return NotFound(response);
            }

            if (response.StatusCode == StatusCodes.Status502BadGateway)
            {
                return StatusCode(StatusCodes.Status502BadGateway, response);
            }

            if (response.StatusCode == StatusCodes.Status504GatewayTimeout)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, response);
            }

            if (response.StatusCode == StatusCodes.Status500InternalServerError)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Dedicated endpoint to verify the server-generated OTP for portals without an external portal URL
        /// and register the applicant portal mapping in tbl_map_user_portal.
        /// </summary>
        /// <remarks>
        /// Sample Request:
        /// 
        ///     POST /api/Applicant/VerifyApplicantOtp
        ///     {
        ///       "portal_id": 5,
        ///       "mobile_no": "9507637005",
        ///       "otp": "123456",
        ///       "deviceid": "DEV123",
        ///       "session": "2026",
        ///       "UserType": "2"
        ///     }
        /// </remarks>
        [HttpPost("VerifyApplicantOtp")]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyApplicantOtp([FromBody] VerifyApplicantOtpRequest request, CancellationToken cancellationToken)
        {
            var response = await _applicantService.VerifyApplicantOtpAsync(request, cancellationToken);

            if (response.StatusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest(response);
            }

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                return NotFound(response);
            }

            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Fetches beneficiary messages from the external portal API (eLabharthi http://10.133.20.151/eLabharthi/api/Beneficiary/Get_Beneficiary_Messages),
        /// saves each message into tbl_Message (including mobile number), and records the synced state in tbl_Message_log.
        /// </summary>
        /// <remarks>
        /// Sample Request:
        /// 
        ///     POST /api/Applicant/GetBeneficiaryMessages
        ///     {
        ///       "token": "408C6E3F4D04A7F7C1608193407356D8"
        ///     }
        /// </remarks>
        [HttpPost("GetBeneficiaryMessages")]
        [ProducesResponseType(typeof(GetBeneficiaryMessagesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> GetBeneficiaryMessages([FromBody] GetBeneficiaryMessagesRequest request, CancellationToken cancellationToken)
        {
            var response = await _applicantService.GetBeneficiaryMessagesAsync(request, cancellationToken);

            if (response.StatusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest(response);
            }

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                return NotFound(response);
            }

            if (response.StatusCode == StatusCodes.Status502BadGateway)
            {
                return StatusCode(StatusCodes.Status502BadGateway, response);
            }

            if (response.StatusCode == StatusCodes.Status504GatewayTimeout)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, response);
            }

            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Fetches pensioner/beneficiary status from the external portal API (eLabharthi http://10.133.20.151/eLabharthi/api/Beneficiary/Get_Beneficiary_Status)
        /// passing portalId, mobileNo, and beneficiaryNo.
        /// </summary>
        /// <remarks>
        /// Sample Request:
        /// 
        ///     POST /api/Applicant/GetBeneficiaryStatus
        ///     {
        ///       "portalId": 1,
        ///       "mobileNo": "9097372728",
        ///       "beneficiaryNo": "000000200577"
        ///     }
        /// </remarks>
        [HttpPost("GetBeneficiaryStatus")]
        [ProducesResponseType(typeof(BeneficiaryStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ApplicantResponse), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> GetBeneficiaryStatus([FromBody] GetBeneficiaryStatusRequest request, CancellationToken cancellationToken)
        {
            var response = await _applicantService.GetBeneficiaryStatusAsync(request, cancellationToken);

            if (response.StatusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest(response);
            }

            if (response.StatusCode == StatusCodes.Status404NotFound)
            {
                return NotFound(response);
            }

            if (response.StatusCode == StatusCodes.Status502BadGateway)
            {
                return StatusCode(StatusCodes.Status502BadGateway, response);
            }

            if (response.StatusCode == StatusCodes.Status504GatewayTimeout)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, response);
            }

            return StatusCode(response.StatusCode, response);
        }
    }
}
