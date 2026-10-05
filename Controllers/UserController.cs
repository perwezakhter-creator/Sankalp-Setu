using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Security;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [AllowAnonymous]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ExceptionLogging _exceptionLogging;

        public UserController(IUserService userService, ExceptionLogging exceptionLogging)
        {
            _userService = userService;
            _exceptionLogging = exceptionLogging;
        }

        // ============================================================
        // REGISTER USER WITH INTERESTS
        // POST /api/User/Register
        // ============================================================
        [AllowAnonymous]
        [HttpPost("Register")]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new RegisterUserResponse
                    {
                        Success = false,
                        Message = "Invalid registration data."
                    });
                }

                var result = await _userService.RegisterUserAsync(request, cancellationToken);
                return StatusCode(result.StatusCode, result.Data);
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Unable to register user."
                });
            }
        }

        // ============================================================
        // LEGACY INSERT USER (Preserved for backwards compatibility)
        // POST /api/User/Insert
        // ============================================================
        [AllowAnonymous]
        [HttpPost("Insert")]
        public IActionResult Insert([FromBody] UserRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // RSA decrypt client key if provided
                string key = string.Empty;
                if (!string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    try
                    {
                        key = RSA_Enc_Dec.Decryption(request.ClientKey);
                    }
                    catch
                    {
                        key = request.ClientKey;
                    }
                }

                // AES decrypt fields (or use plain text if not encrypted / no key)
                string Username = !string.IsNullOrWhiteSpace(key)
                    ? Aes256CbcEncrypterApp.Decrypt(request.Username ?? "", key)
                    : (request.Username ?? "");

                string Password = !string.IsNullOrWhiteSpace(key)
                    ? Aes256CbcEncrypterApp.Decrypt(request.Password ?? "", key)
                    : (request.Password ?? "");

                string mobile = !string.IsNullOrWhiteSpace(key)
                    ? Aes256CbcEncrypterApp.Decrypt(request.Mobile ?? "", key)
                    : (request.Mobile ?? "");

                if (string.IsNullOrWhiteSpace(Username))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Username is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Password is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Mobile number is required."
                    });
                }

                var enc = new Encryptor(Encryptor.PrivateKey.ToString());
                string encPassword = enc.Encrypt(Password);

                // Database
                var result = _userService.InsertUser(Username, encPassword, mobile);

                if (result.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Unable to create user."
                    });
                }

                bool status = false;
                string message = "";

                if (result.Rows[0][0] is Array objArr && objArr.Length > 0)
                {
                    status = ParseBool(objArr.GetValue(0));
                    if (objArr.Length > 1)
                    {
                        message = FormatValue(objArr.GetValue(1));
                    }
                    else
                    {
                        message = status ? "User registered successfully." : "Unable to create user.";
                    }
                }
                else
                {
                    if (result.Columns.Contains("Status") && result.Rows[0]["Status"] != DBNull.Value)
                    {
                        status = ParseBool(result.Rows[0]["Status"]);
                    }
                    else if (result.Columns.Contains("status") && result.Rows[0]["status"] != DBNull.Value)
                    {
                        status = ParseBool(result.Rows[0]["status"]);
                    }

                    if (result.Columns.Contains("Message") && result.Rows[0]["Message"] != DBNull.Value)
                    {
                        message = FormatValue(result.Rows[0]["Message"]);
                    }
                    else if (result.Columns.Contains("message") && result.Rows[0]["message"] != DBNull.Value)
                    {
                        message = FormatValue(result.Rows[0]["message"]);
                    }
                    else if (result.Columns.Contains("msg") && result.Rows[0]["msg"] != DBNull.Value)
                    {
                        message = FormatValue(result.Rows[0]["msg"]);
                    }

                    if (string.IsNullOrEmpty(message) && result.Columns.Count == 1 && result.Rows[0][0] != DBNull.Value)
                    {
                        string rawVal = FormatValue(result.Rows[0][0]).Trim();
                        if (rawVal.StartsWith("(") && rawVal.EndsWith(")"))
                        {
                            string inner = rawVal.Substring(1, rawVal.Length - 2);
                            var parts = inner.Split(',', 2);
                            if (parts.Length > 0)
                            {
                                status = ParseBool(parts[0].Trim().Trim('"'));
                            }
                            if (parts.Length > 1)
                            {
                                message = parts[1].Trim().Trim('"');
                            }
                        }
                        else if (!result.Columns.Contains("Status") && !result.Columns.Contains("status"))
                        {
                            message = rawVal;
                        }
                    }
                    else if (result.Columns.Count > 1 && string.IsNullOrEmpty(message) && result.Rows[0][1] != DBNull.Value)
                    {
                        message = FormatValue(result.Rows[0][1]);
                    }
                }

                return Ok(new
                {
                    status,
                    message
                });
            }
            catch (Exception ex)
            {
                _exceptionLogging.SendErrorToText(ex);

                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        private static string FormatValue(object? val)
        {
            if (val == null || val == DBNull.Value)
                return string.Empty;

            if (val is Array arr)
            {
                var list = new List<string>();
                foreach (var item in arr)
                {
                    if (item != null && item != DBNull.Value)
                        list.Add(item.ToString() ?? string.Empty);
                }
                return string.Join(", ", list);
            }

            return val.ToString() ?? string.Empty;
        }

        private static bool ParseBool(object? val)
        {
            if (val == null || val == DBNull.Value)
                return false;

            if (val is bool b)
                return b;

            if (val is int i)
                return i != 0;

            if (val is long l)
                return l != 0;

            if (val is short s)
                return s != 0;

            if (val is byte by)
                return by != 0;

            try
            {
                return Convert.ToBoolean(val);
            }
            catch
            {
                string str = val.ToString()?.Trim() ?? string.Empty;
                return str == "1" ||
                       str.Equals("t", StringComparison.OrdinalIgnoreCase) ||
                       str.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       str.Equals("success", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
