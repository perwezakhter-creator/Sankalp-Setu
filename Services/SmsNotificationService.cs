using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Services
{
    public class SmsNotificationService : ISmsNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<SmsNotificationService> _logger;
        private readonly ExceptionLogging _exceptionLogging;

        public SmsNotificationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<SmsNotificationService> logger,
            ExceptionLogging exceptionLogging)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _exceptionLogging = exceptionLogging;
        }

        public int GetOtpExpiryMinutes()
        {
            return _config.GetValue<int>("SmsSettings:OtpExpiryMinutes", 10);
        }

        public async Task<bool> SendOtpSmsAsync(string mobileNo, string otp)
        {
            var (success, _) = await SendOtpAsync(mobileNo, otp);
            return success;
        }

        public async Task<(bool Success, string Message)> SendOtpAsync(string mobileNo, string otp)
        {
            try
            {
                string apiUrl = _config["SmsSettings:ApiUrl"] ?? string.Empty;
                string username = _config["SmsSettings:Username"] ?? string.Empty;
                string password = _config["SmsSettings:Password"] ?? string.Empty;
                string secureKey = _config["SmsSettings:SecureKey"] ?? string.Empty;
                string templateId = _config["SmsSettings:TemplateId"] ?? string.Empty;

                int expiryMinutes = GetOtpExpiryMinutes();
                // DLT approved template for TemplateId: 1307173675486925693
                string message = $"OTP: {otp}. Please Enter This OTP Before Submit Your Application Form. Health Department - Bihar Government";

                if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("SMS settings are incomplete. Skipping external SMS dispatch for Mobile={Mobile}", mobileNo);
                    return (false, "SMS settings are incomplete.");
                }

                var payload = new
                {
                    username = username,
                    password = password,
                    secureKey = secureKey,
                    mobileNo = mobileNo.Trim(),
                    message = message,
                    templateId = templateId
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);

                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("SMS dispatch response status: {StatusCode}, body: {Body}", response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<SmsApiResponse>(responseBody);
                        if (result != null)
                        {
                            if (result.Success || (!string.IsNullOrWhiteSpace(result.Response) && result.Response.Contains("402")))
                            {
                                return (true, "OTP sent successfully.");
                            }

                            string errorMsg = !string.IsNullOrWhiteSpace(result.Response)
                                ? result.Response
                                : (!string.IsNullOrWhiteSpace(result.Error) ? result.Error : "Failed to send OTP SMS.");

                            _logger.LogWarning("SMS gateway responded with error: {Error}", errorMsg);
                            return (false, errorMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not parse SMS gateway response JSON: {Body}", responseBody);
                    }

                    return (true, "OTP sent successfully.");
                }

                return (false, $"SMS gateway returned HTTP status {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP SMS to {Mobile}", mobileNo);
                _exceptionLogging.SendErrorToText(ex);
                return (false, ex.Message);
            }
        }
    }
}
