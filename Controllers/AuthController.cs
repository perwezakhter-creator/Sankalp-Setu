using SankalpAPI.Interface;
using SankalpAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            LoginResponse response = await _authService.LoginAsync(request);

            if (!response.Status)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }
    }
}
