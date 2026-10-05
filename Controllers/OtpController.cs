using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SankalpAPI.DTO;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using VerifyOtpRequest = SankalpAPI.DTO.VerifyOtpRequest;
using VerifyOtpResponse = SankalpAPI.DTO.VerifyOtpResponse;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [AllowAnonymous]
    public class OtpController : ControllerBase
    {
        private readonly IOtpService _otpService;

        public OtpController(IOtpService otpService)
        {
            _otpService = otpService;
        }

        /// <summary>
        /// Sends an OTP to the specified mobile number via Bihar Health SMS Gateway and records it in the database.
        /// </summary>
        /// <param name="request">Request containing 10-digit mobile number, optional device ID, and optional client encryption key</param>
        /// <returns>Response with delivery status and OTP validity information</returns>
        [HttpPost("SendOtp")]
        [ProducesResponseType(typeof(OtpSendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OtpSendResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OtpSendResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var response = await _otpService.SendOtpAsync(request);

            if (!response.Status)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Verifies the OTP entered by the user. If verified, checks whether user already exists in tbl_user.
        /// If user exists, validates/updates DeviceId and returns existing user information. If not, returns userExists = false so registration can continue.
        /// </summary>
        /// <param name="request">Request containing 10-digit mobile number, entered OTP, optional device ID, and optional client encryption key</param>
        /// <returns>Response indicating OTP validation and user existence status</returns>
        [HttpPost("VerifyOtp")]
        [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (request == null)
            {
                return BadRequest(new VerifyOtpResponse
                {
                    Success = false,
                    UserExists = false,
                    Message = "Invalid or expired OTP.",
                    Data = null
                });
            }

            var response = await _otpService.VerifyOtpAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
