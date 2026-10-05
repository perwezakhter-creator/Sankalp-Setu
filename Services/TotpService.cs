using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Security;

namespace SankalpAPI.Services
{
    public class TotpService
    {
        private readonly ITotpRepository _totpRepository;
        private readonly IPortalRepository _portalRepository;
        private readonly ISmsNotificationService _smsService;
        private readonly IHostEnvironment _env;
        private readonly ILogger<TotpService> _logger;
        private readonly ExceptionLogging _exceptionLogging;

        public TotpService(
            ITotpRepository totpRepository,
            IPortalRepository portalRepository,
            ISmsNotificationService smsService,
            IHostEnvironment env,
            ILogger<TotpService> logger,
            ExceptionLogging exceptionLogging)
        {
            _totpRepository = totpRepository;
            _portalRepository = portalRepository;
            _smsService = smsService;
            _env = env;
            _logger = logger;
            _exceptionLogging = exceptionLogging;
        }

        public async Task<InitiateRegistrationResponse> InitiateRegistrationAsync(InitiateRegistrationRequest request)
        {
            if (request == null)
            {
                return new InitiateRegistrationResponse
                {
                    Status = false,
                    Message = "Invalid request."
                };
            }

            string mobile = DecryptIfNeeded(request.Mobile, request.ClientKey);
            string? userId = !string.IsNullOrWhiteSpace(request.UserId)
                ? DecryptIfNeeded(request.UserId, request.ClientKey)
                : null;

            if (request.PortalId <= 0)
            {
                return new InitiateRegistrationResponse
                {
                    Status = false,
                    Message = "Valid Portal ID is required."
                };
            }

            if (string.IsNullOrWhiteSpace(mobile))
            {
                return new InitiateRegistrationResponse
                {
                    Status = false,
                    Message = "Mobile number is required."
                };
            }

            try
            {
                // 1. Verify Portal exists
                var portal = await _portalRepository.GetPortalByIdAsync(request.PortalId);
                if (portal == null)
                {
                    return new InitiateRegistrationResponse
                    {
                        Status = false,
                        Message = "Selected web-application does not exist."
                    };
                }

                // 2. Check user existence (Web-app is source of truth for user existence)
                var userInfo = await _totpRepository.CheckUserExistsAsync(request.PortalId, mobile, userId);
                if (!userInfo.Exists)
                {
                    return new InitiateRegistrationResponse
                    {
                        Status = false,
                        Message = "User does not exist in the selected web-application."
                    };
                }

                // 3. Generate 6-digit OTP
                int otpNumber = RandomNumberGenerator.GetInt32(100000, 1000000);
                string otp = otpNumber.ToString();

                // 4. Save OTP in database
                await _totpRepository.SaveOtpAsync(mobile, otp, request.DeviceId);

                // 5. Send OTP via SMS
                bool smsSent = await _smsService.SendOtpSmsAsync(mobile, otp);

                int expiryMinutes = _smsService.GetOtpExpiryMinutes();
                string masked = MaskMobile(mobile);

                return new InitiateRegistrationResponse
                {
                    Status = true,
                    Message = "User exists. OTP sent successfully to registered mobile number.",
                    Data = new InitiateRegistrationData
                    {
                        PortalId = portal.PortalId,
                        PortalName = portal.PortalName,
                        Mobile = mobile,
                        MaskedMobile = masked,
                        OtpExpiryMinutes = expiryMinutes,
                        DevOtp = _env.IsDevelopment() ? otp : null
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating registration for mobile {Mobile}", mobile);
                _exceptionLogging.SendErrorToText(ex);

                return new InitiateRegistrationResponse
                {
                    Status = false,
                    Message = "Unable to process registration request."
                };
            }
        }

        public async Task<ValidateOtpAndGenerateSecretResponse> ValidateOtpAndGenerateSecretAsync(ValidateOtpAndGenerateSecretRequest request)
        {
            if (request == null)
            {
                return new ValidateOtpAndGenerateSecretResponse
                {
                    Status = false,
                    Message = "Invalid request."
                };
            }

            string mobile = DecryptIfNeeded(request.Mobile, request.ClientKey);
            string otp = DecryptIfNeeded(request.Otp, request.ClientKey);
            string? userId = !string.IsNullOrWhiteSpace(request.UserId)
                ? DecryptIfNeeded(request.UserId, request.ClientKey)
                : null;

            if (request.PortalId <= 0)
            {
                return new ValidateOtpAndGenerateSecretResponse
                {
                    Status = false,
                    Message = "Valid Portal ID is required."
                };
            }

            if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(otp))
            {
                return new ValidateOtpAndGenerateSecretResponse
                {
                    Status = false,
                    Message = "Mobile and OTP are required."
                };
            }

            try
            {
                // 1. Get Portal
                var portal = await _portalRepository.GetPortalByIdAsync(request.PortalId);
                if (portal == null)
                {
                    return new ValidateOtpAndGenerateSecretResponse
                    {
                        Status = false,
                        Message = "Selected web-application does not exist."
                    };
                }

                // 2. Validate OTP
                int expiryMinutes = _smsService.GetOtpExpiryMinutes();
                bool isValid = await _totpRepository.ValidateOtpAsync(mobile, otp.Trim(), expiryMinutes);

                if (!isValid)
                {
                    return new ValidateOtpAndGenerateSecretResponse
                    {
                        Status = false,
                        Message = "Invalid or expired OTP."
                    };
                }

                // 3. Create TOTP Secret (Base32 encoded 160-bit key)
                string totpSecret = TotpHelper.GenerateBase32Secret(20);

                // 4. Save secret in tbl_map_user_portal
                string entryBy = !string.IsNullOrWhiteSpace(userId) ? userId : "MobileApp";
                await _totpRepository.SaveTotpSecretAsync(portal.PortalId, mobile, userId, totpSecret, null, entryBy);

                // 5. Generate QR Code URI
                string accountName = !string.IsNullOrWhiteSpace(userId) ? userId : mobile;
                string qrCodeUri = TotpHelper.GenerateOtpauthUri(portal.PortalName, accountName, totpSecret);

                return new ValidateOtpAndGenerateSecretResponse
                {
                    Status = true,
                    Message = "OTP validated successfully. TOTP secret generated and registered.",
                    Data = new TotpSecretData
                    {
                        PortalId = portal.PortalId,
                        PortalName = portal.PortalName,
                        Mobile = mobile,
                        UserId = accountName,
                        TotpSecret = totpSecret,
                        Digits = 6,
                        Period = 30,
                        Algorithm = "SHA1",
                        QrCodeUri = qrCodeUri
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP for mobile {Mobile}", mobile);
                _exceptionLogging.SendErrorToText(ex);

                return new ValidateOtpAndGenerateSecretResponse
                {
                    Status = false,
                    Message = "Unable to validate OTP and generate secret."
                };
            }
        }

        public async Task<ValidateTotpResponse> ValidateTotpAsync(ValidateTotpRequest request)
        {
            if (request == null)
            {
                return new ValidateTotpResponse
                {
                    Status = false,
                    Message = "Invalid request."
                };
            }

            if (request.PortalId <= 0 || string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.TotpCode))
            {
                return new ValidateTotpResponse
                {
                    Status = false,
                    Message = "Portal ID, Mobile number, and TOTP code are required."
                };
            }

            try
            {
                string? secret = await _totpRepository.GetTotpSecretAsync(request.PortalId, request.Mobile.Trim());

                if (string.IsNullOrWhiteSpace(secret))
                {
                    return new ValidateTotpResponse
                    {
                        Status = false,
                        Message = "TOTP secret is not registered for this mobile and web-application."
                    };
                }

                bool isValid = TotpHelper.ValidateTotp(secret, request.TotpCode.Trim(), window: 1);

                if (isValid)
                {
                    return new ValidateTotpResponse
                    {
                        Status = true,
                        Message = "TOTP verified successfully.",
                        Data = new
                        {
                            portalId = request.PortalId,
                            mobile = request.Mobile,
                            verifiedAt = DateTime.UtcNow
                        }
                    };
                }

                return new ValidateTotpResponse
                {
                    Status = false,
                    Message = "Invalid or expired TOTP code."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TOTP code for mobile {Mobile}", request.Mobile);
                _exceptionLogging.SendErrorToText(ex);

                return new ValidateTotpResponse
                {
                    Status = false,
                    Message = "An error occurred while validating TOTP."
                };
            }
        }

        private static string DecryptIfNeeded(string text, string? clientKey)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(clientKey))
                return text;

            try
            {
                string key;
                try
                {
                    key = RSA_Enc_Dec.Decryption(clientKey);
                }
                catch
                {
                    key = clientKey;
                }

                return Aes256CbcEncrypterApp.Decrypt(text, key);
            }
            catch
            {
                return text;
            }
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4)
                return mobile;

            return new string('X', mobile.Length - 4) + mobile.Substring(mobile.Length - 4);
        }
    }
}
