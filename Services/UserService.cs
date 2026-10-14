using SankalpAPI.DTO;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using System.Data;
using System.Text.RegularExpressions;

namespace SankalpAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly ExceptionLogging _exceptionLogging;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, ExceptionLogging exceptionLogging)
        {
            _userRepository = userRepository;
            _logger = logger;
            _exceptionLogging = exceptionLogging;
        }

        // ============================================================
        // REGISTER USER
        // ============================================================
        public async Task<ServiceResult<RegisterUserResponse>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            // --------------------------------------------------------
            // 1. Model Validation
            // --------------------------------------------------------
            if (request == null)
            {
                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Invalid registration data."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MobileNo))
            {
                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Invalid registration data."
                });
            }

            // Mobile number format validation (10 digits)
            string cleanedMobile = request.MobileNo.Trim();
            if (!Regex.IsMatch(cleanedMobile, @"^[0-9]{10}$"))
            {
                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Invalid registration data."
                });
            }
            request.MobileNo = cleanedMobile;
            request.Name = request.Name.Trim();

            // --------------------------------------------------------
            // 1b. Optional FCM Device & Platform Validation
            // --------------------------------------------------------
            request.DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim();
            request.FcmToken = string.IsNullOrWhiteSpace(request.FcmToken) ? null : request.FcmToken.Trim();
            request.Platform = string.IsNullOrWhiteSpace(request.Platform) ? "Android" : request.Platform.Trim();

            if (request.DeviceId != null && request.DeviceId.Length > 200)
            {
                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Invalid registration data."
                });
            }

            if (request.FcmToken != null)
            {
                if (string.IsNullOrWhiteSpace(request.DeviceId) ||
                    request.FcmToken.Length < 10 ||
                    request.FcmToken.Length > 2048 ||
                    string.Equals(request.FcmToken, "null", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.FcmToken, "undefined", StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                    {
                        Success = false,
                        Message = "Invalid registration data."
                    });
                }

                if (request.Platform.Length > 20 ||
                    (!string.Equals(request.Platform, "Android", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(request.Platform, "iOS", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(request.Platform, "Web", StringComparison.OrdinalIgnoreCase)))
                {
                    return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status400BadRequest, new RegisterUserResponse
                    {
                        Success = false,
                        Message = "Invalid registration data."
                    });
                }

                request.Platform = string.Equals(request.Platform, "android", StringComparison.OrdinalIgnoreCase)
                    ? "Android"
                    : (string.Equals(request.Platform, "ios", StringComparison.OrdinalIgnoreCase) ? "iOS" : "Web");
            }

            // --------------------------------------------------------
            // 2. Password Processing
            // If password provided, encrypt using Encryptor; else empty string
            // --------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                try
                {
                    var enc = new Encryptor(Encryptor.PrivateKey.ToString());
                    request.Password = enc.Encrypt(request.Password);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to encrypt password, using fallback plain string.");
                }
            }
            else
            {
                request.Password = string.Empty;
            }

            // --------------------------------------------------------
            // 3. Deduplicate Interests
            // Ensure no duplicate IDs are passed down to SQL Server
            // --------------------------------------------------------
            request.Interests = request.Interests?.Where(id => id > 0).Distinct().ToList() ?? new List<long>();

            // --------------------------------------------------------
            // 4. Execute atomic repository transaction
            // --------------------------------------------------------
            try
            {
                var (isSuccess, isDuplicate, userId, errorMessage) =
                    await _userRepository.RegisterUserWithInterestsAsync(request, cancellationToken);

                // Duplicate mobile check failure
                if (isDuplicate)
                {
                    return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status409Conflict, new RegisterUserResponse
                    {
                        Success = false,
                        Message = "Mobile number already registered."
                    });
                }

                // General database or processing failure
                if (!isSuccess)
                {
                    return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status500InternalServerError, new RegisterUserResponse
                    {
                        Success = false,
                        Message = "Unable to register user."
                    });
                }

                // Success (201 Created)
                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status201Created, new RegisterUserResponse
                {
                    Success = true,
                    Message = "User registered successfully.",
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UserService.RegisterUserAsync for {Mobile}", request.MobileNo);
                _exceptionLogging.SendErrorToText(ex);

                return ServiceResult<RegisterUserResponse>.Ok(StatusCodes.Status500InternalServerError, new RegisterUserResponse
                {
                    Success = false,
                    Message = "Unable to register user."
                });
            }
        }

        // ============================================================
        // LEGACY INSERT USER (Preserved for backwards compatibility)
        // ============================================================
        public DataTable InsertUser(string Username, string Password, string mobile)
        {
            return _userRepository.InsertUser(Username, Password, mobile);
        }
    }
}
