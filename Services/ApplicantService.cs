using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Security;

namespace SankalpAPI.Services
{
    /// <summary>
    /// Service to communicate with external portal APIs to fetch applicant details.
    /// Returns standardized { status, message, data } format.
    /// </summary>
    public class ApplicantService : IApplicantService
    {
        private const string SendOtpEndpoint = "/api/Beneficiary/send-otp";
        private const string VerifyOtpEndpoint = "/api/Beneficiary/verify-otp";
        private const string GetBeneficiaryMessagesEndpoint = "/api/Beneficiary/Get_Beneficiary_Messages";
        private const string DefaultPortalUrl = "http://10.133.20.151/eLabharthiAPI";
        private const int DefaultTimeoutSeconds = 15;

        private readonly IPortalRepository _portalRepository;
        private readonly IApplicantRepository _applicantRepository;
        private readonly ClientTokenService _clientTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApplicantService> _logger;
        private readonly ExceptionLogging _exceptionLogging;

        public ApplicantService(
            IPortalRepository portalRepository, 
            IApplicantRepository applicantRepository, 
            ClientTokenService clientTokenService,
            IHttpClientFactory httpClientFactory, 
            ILogger<ApplicantService> logger, 
            ExceptionLogging exceptionLogging)
        {
            _portalRepository = portalRepository;
            _applicantRepository = applicantRepository;
            _clientTokenService = clientTokenService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _exceptionLogging = exceptionLogging;
        }

        public async Task<ApplicantResponse> GetApplicantDetailsAsync(ApplicantRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Request payload is required.",
                    Data = null
                };
            }

            string mobileNo = request.MobileNo?.Trim() ?? string.Empty;
            string deviceId = request.DeviceId?.Trim() ?? string.Empty;
            string session = request.Session?.Trim() ?? string.Empty;
            string userType = request.UserType?.Trim() ?? string.Empty;

            // Validate mandatory parameters
            if (request.PortalId < 0)
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "A valid portal_id is required.",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(mobileNo))
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "A valid mobile number is required.",
                    Data = null
                };
            }

            try
            {
                // 4. Resolve portal information from database (mst_portal & tbl_map_user_portal)
                var mapping = await _portalRepository.GetPortalAndUserMappingAsync(request.PortalId, mobileNo);
                if (mapping == null)
                {
                    // Fallback to default eLabharthi portal if portal ID is 0 or 1
                    if (request.PortalId == 0 || request.PortalId == 1)
                    {
                        mapping = new PortalUserMappingDto
                        {
                            PortalId = request.PortalId,
                            PortalName = "eLabharthi",
                            PortalUrl = DefaultPortalUrl,
                            IsActive = "Y"
                        };
                    }
                    else
                    {
                        return new ApplicantResponse
                        {
                            Status = false,
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Message = $"Portal ID {request.PortalId} was not found in mst_portal.",
                            Data = null
                        };
                    }
                }

                // 5. Validate portal active state
                if (!string.IsNullOrWhiteSpace(mapping.IsActive) &&
                    (mapping.IsActive.Equals("N", StringComparison.OrdinalIgnoreCase) || mapping.IsActive == "0"))
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = $"Portal '{mapping.PortalName}' (ID {mapping.PortalId}) is currently marked inactive.",
                        Data = null
                    };
                }

                // 6. Construct target external API URL
                string targetUrl = BuildTargetUrl(mapping.PortalUrl, SendOtpEndpoint);
                if (string.IsNullOrWhiteSpace(targetUrl) || !Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = $"Invalid portal URL configured for portal '{mapping.PortalName}': '{targetUrl}'",
                        Data = null
                    };
                }

                // 7. Build HTTP Request
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Inject Bearer token from user-portal mapping if available
                if (!string.IsNullOrWhiteSpace(mapping.Token))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {mapping.Token.Trim()}");
                }

                // Inject Secret Key from user-portal mapping if available
                if (!string.IsNullOrWhiteSpace(mapping.SecretKey))
                {
                    httpRequest.Headers.TryAddWithoutValidation("X-Secret-Key", mapping.SecretKey.Trim());
                }

                // Correlation ID for distributed tracing
                httpRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", Guid.NewGuid().ToString("N"));

                // Prepare forwarded payload matching external API expected schema: { portType, mobileNo, deviceId, session }
                var forwardPayload = new Dictionary<string, object?>
                {
                    ["portalId"] = request.PortalId,
                    ["mobileNo"] = request.MobileNo,
                    ["deviceId"] = request.DeviceId,
                    ["session"] = request.Session,
                    ["userType"] = request.UserType
                };

                string jsonContent = JsonConvert.SerializeObject(forwardPayload);
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 8. Execute external HTTP call
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

                _logger.LogInformation("Accessing external API for portal '{PortalName}' (ID {PortalId}) at {TargetUrl} for mobile {Mobile}",
                    mapping.PortalName, mapping.PortalId, targetUri, mobileNo);

                using var response = await client.SendAsync(httpRequest, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                int statusCode = (int)response.StatusCode;

                _logger.LogInformation("External portal response received with status code {StatusCode}", statusCode);

                // 8. Handle HTTP Response
                if (response.IsSuccessStatusCode)
                {
                    bool isStatus = true;
                    string message = "Applicant details retrieved successfully.";
                    object? dataPayload = null;

                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        try
                        {
                            var token = JToken.Parse(responseBody);
                            if (token is JObject obj)
                            {
                                if (obj["status"] != null)
                                {
                                    isStatus = obj["status"]!.Value<bool>();
                                }
                                if (obj["message"] != null)
                                {
                                    message = obj["message"]!.ToString();
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    // Extract strongly-typed beneficiary records
                    var beneficiaries = ExtractBeneficiaries(responseBody);
                    if (beneficiaries != null && beneficiaries.Count > 0)
                    {
                        dataPayload = beneficiaries;

                        if (isStatus)
                        {
                            try
                            {
                                await _applicantRepository.SaveBeneficiaryDetailsAsync(mapping.PortalId, mobileNo, deviceId, session, userType, beneficiaries, responseBody);

                                _logger.LogInformation("Saved {Count} beneficiary record(s) to database for mobile {Mobile}",
                                    beneficiaries.Count, mobileNo);
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogError(dbEx, "Failed to save beneficiary records to database for mobile {Mobile}", mobileNo);
                                _exceptionLogging.SendErrorToText(dbEx);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        // Fallback: parse using System.Text.Json so it serializes cleanly without JToken nesting
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
                            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var dataProp))
                            {
                                dataPayload = dataProp.Clone();
                            }
                            else
                            {
                                dataPayload = doc.RootElement.Clone();
                            }
                        }
                        catch
                        {
                            dataPayload = new List<object>();
                        }
                    }
                    else
                    {
                        dataPayload = new List<object>();
                    }

                    return new ApplicantResponse
                    {
                        Status = isStatus,
                        StatusCode = statusCode,
                        Message = message,
                        Data = dataPayload
                    };
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    string notFoundMessage = "Applicant details not found on external portal.";
                    try
                    {
                        var token = JToken.Parse(responseBody);
                        if (token is JObject obj && obj["message"] != null)
                        {
                            notFoundMessage = obj["message"]!.ToString();
                        }
                    }
                    catch { }

                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Message = notFoundMessage,
                        Data = null
                    };
                }

                string errMessage = $"External portal returned HTTP {statusCode}.";
                try
                {
                    var token = JToken.Parse(responseBody);
                    if (token is JObject obj && obj["message"] != null)
                    {
                        errMessage = obj["message"]!.ToString();
                    }
                }
                catch { }

                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = statusCode,
                    Message = errMessage,
                    Data = null
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout connecting to external portal ID {PortalId}", request.PortalId);
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.GatewayTimeout,
                    Message = "Gateway Timeout: The external portal took too long to respond.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to external portal ID {PortalId}", request.PortalId);
                _exceptionLogging.SendErrorToText(ex);

                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Message = $"Bad Gateway: Unable to connect to external portal API ({ex.Message})",
                    Data = null
                };
            }
        }

        public async Task<ApplicantResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Request payload is required.",
                    Data = null
                };
            }

            // 1. Decrypt parameters if encrypted
            string clientKey = DecryptClientKey(request.ClientKey);
            string mobileNo = DecryptField(request.MobileNo, clientKey);
            string otp = DecryptField(request.Otp, clientKey);
            string? deviceId = DecryptField(request.DeviceId, clientKey);
            string? userId = DecryptField(request.UserId, clientKey);
            string? session = DecryptField(request.Session, clientKey);

            // 2. Validate mandatory parameters
            if (request.PortalId < 0)
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "A valid portal_id is required.",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(mobileNo))
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "A valid mobile number is required.",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(otp))
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "A valid OTP is required.",
                    Data = null
                };
            }

            try
            {
                // 3. Resolve portal to verify portal exists and get name/url
                var mapping = await _portalRepository.GetPortalAndUserMappingAsync(request.PortalId, mobileNo);
                if (mapping == null)
                {
                    // Fallback to default eLabharthi portal if portal ID is 0 or 1
                    if (request.PortalId == 0 || request.PortalId == 1)
                    {
                        mapping = new PortalUserMappingDto
                        {
                            PortalId = request.PortalId,
                            PortalName = "eLabharthi",
                            PortalUrl = DefaultPortalUrl,
                            IsActive = "Y"
                        };
                    }
                    else
                    {
                        return new ApplicantResponse
                        {
                            Status = false,
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Message = $"Portal ID {request.PortalId} was not found in mst_portal.",
                            Data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(mapping.IsActive) && (mapping.IsActive.Equals("N", StringComparison.OrdinalIgnoreCase) || mapping.IsActive == "0"))
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = $"Portal '{mapping.PortalName}' (ID {mapping.PortalId}) is currently marked inactive.",
                        Data = null
                    };
                }

                // 4. Extract list of a_Id(s)
                var aIdList = new List<long>();
                if (request.A_Id != null)
                {
                    foreach (var id in request.A_Id)
                    {
                        if (id > 0 && !aIdList.Contains(id))
                        {
                            aIdList.Add(id);
                        }
                    }
                }

                // If no a_Id passed, attempt fallback to tbl_beneficiary_details
                if (aIdList.Count == 0)
                {
                    var (storedAId, storedSession, storedDeviceId, storedBenNo) = 
                        await _applicantRepository.GetLatestBeneficiaryInfoAsync(request.PortalId, mobileNo, null);

                    if (storedAId > 0)
                    {
                        aIdList.Add(storedAId);
                    }
                    if (string.IsNullOrWhiteSpace(session) && !string.IsNullOrWhiteSpace(storedSession))
                    {
                        session = storedSession;
                    }
                    if (string.IsNullOrWhiteSpace(deviceId) && !string.IsNullOrWhiteSpace(storedDeviceId))
                    {
                        deviceId = storedDeviceId;
                    }
                    if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(storedBenNo))
                    {
                        userId = storedBenNo;
                    }
                }

                if (aIdList.Count == 0)
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = "A_Id is required. Please provide a_Id or ensure applicant details were fetched first.",
                        Data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(session))
                {
                    session = DateTime.Now.Year.ToString();
                }

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceId = "DEFAULT_DEVICE";
                }

                // 5. Resolve target external VerifyOtp URL
                string targetUrl = BuildTargetUrl(mapping.PortalUrl, VerifyOtpEndpoint);
                if (string.IsNullOrWhiteSpace(targetUrl) || !Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = $"Invalid verify-otp URL configured for portal '{mapping.PortalName}': '{targetUrl}'",
                        Data = null
                    };
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

                var verifiedResults = new List<VerifyOtpData>();
                string lastErrorMessage = "Invalid or expired OTP.";
                int lastStatusCode = (int)HttpStatusCode.BadRequest;
                bool anySuccess = false;

                // 6. Call external VerifyOtp API with array of a_Id
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrWhiteSpace(mapping.Token))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {mapping.Token.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(mapping.SecretKey))
                {
                    httpRequest.Headers.TryAddWithoutValidation("X-Secret-Key", mapping.SecretKey.Trim());
                }

                httpRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", Guid.NewGuid().ToString("N"));

                var forwardPayload = new Dictionary<string, object?>
                {
                    ["a_Id"] = aIdList,
                    ["mobileNo"] = mobileNo,
                    ["otp"] = otp,
                    ["deviceId"] = deviceId,
                    ["session"] = session
                };

                string jsonContent = JsonConvert.SerializeObject(forwardPayload);
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling verify-otp external API for portal '{PortalName}' (ID {PortalId}) at {TargetUrl} for mobile {Mobile}, a_Id [{AIds}]",
                    mapping.PortalName, mapping.PortalId, targetUri, mobileNo, string.Join(", ", aIdList));

                using var response = await client.SendAsync(httpRequest, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                int statusCode = (int)response.StatusCode;

                _logger.LogInformation("External verify-otp response: HTTP {StatusCode}, Body: {ResponseBody}",
                    statusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    bool rootStatus = true;
                    string message = "OTP verified successfully.";
                    var dataItems = new List<JObject>();

                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        try
                        {
                            var token = JToken.Parse(responseBody);
                            if (token is JObject obj)
                            {
                                if (obj["status"] != null)
                                {
                                    rootStatus = obj["status"]!.Value<bool>();
                                }
                                if (obj["message"] != null)
                                {
                                    message = obj["message"]!.ToString();
                                }
                                if (obj["data"] is JArray dataArr)
                                {
                                    foreach (var item in dataArr)
                                    {
                                        if (item is JObject itemObj)
                                        {
                                            dataItems.Add(itemObj);
                                        }
                                    }
                                }
                                else if (obj["data"] is JObject dataObj)
                                {
                                    dataItems.Add(dataObj);
                                }
                            }
                            else if (token is JArray rootArr)
                            {
                                foreach (var item in rootArr)
                                {
                                    if (item is JObject itemObj)
                                    {
                                        dataItems.Add(itemObj);
                                    }
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed to parse external verify-otp response body.");
                        }
                    }

                    if (dataItems.Count > 0)
                    {
                        foreach (var dObj in dataItems)
                        {
                            bool isItemVerified = false;
                            if (dObj["isVerify"] != null)
                            {
                                isItemVerified = dObj["isVerify"]!.Value<bool>();
                            }
                            else if (dObj["is_verified"] != null)
                            {
                                isItemVerified = dObj["is_verified"]!.Value<bool>();
                            }
                            else if (dObj["is_verify"] != null)
                            {
                                isItemVerified = dObj["is_verify"]!.Value<bool>();
                            }
                            else if (dObj["status"] != null)
                            {
                                isItemVerified = dObj["status"]!.Value<bool>();
                            }
                            else
                            {
                                isItemVerified = rootStatus;
                            }

                            long extAId = 0;
                            if (dObj["a_Id"] != null || dObj["a_id"] != null || dObj["A_Id"] != null)
                            {
                                extAId = (dObj["a_Id"] ?? dObj["a_id"] ?? dObj["A_Id"])!.Value<long>();
                            }

                            string? itemMessage = dObj["message"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(itemMessage))
                            {
                                lastErrorMessage = itemMessage;
                            }

                            if (isItemVerified)
                            {
                                anySuccess = true;

                                string? extBeneficiaryId = (dObj["beneficiaryID"] ?? dObj["beneficiaryId"] ?? dObj["BeneficiaryID"])?.ToString();
                                string? extSecretKey = (dObj["secretKey"] ?? dObj["SecretKey"])?.ToString();
                                string? extToken = (dObj["token"] ?? dObj["Token"])?.ToString();
                                DateTime? extVerifyDate = null;
                                if (dObj["verifyDate"] != null || dObj["VerifyDate"] != null)
                                {
                                    if (DateTime.TryParse((dObj["verifyDate"] ?? dObj["VerifyDate"])!.ToString(), out var parsedDate))
                                    {
                                        extVerifyDate = parsedDate;
                                    }
                                }

                                long finalAId = extAId > 0 ? extAId : (aIdList.Count == 1 ? aIdList[0] : 0);

                                string finalUserId = !string.IsNullOrWhiteSpace(userId)? userId.Trim(): (!string.IsNullOrWhiteSpace(extBeneficiaryId) ? extBeneficiaryId.Trim() : mobileNo.Trim());

                                //string secretKey = !string.IsNullOrWhiteSpace(extSecretKey)? extSecretKey.Trim(): TotpHelper.GenerateBase32Secret(20);
                                string secretKey = string.Empty;
                                if(!string.IsNullOrWhiteSpace(extSecretKey))
                                {
                                    secretKey = extSecretKey.Trim();
                                }
                                else
                                {
                                    secretKey = TotpHelper.GenerateBase32Secret(20);
                                }

                                //string clientToken = !string.IsNullOrWhiteSpace(extToken)? extToken.Trim(): _clientTokenService.GenerateToken();                               

                                string clientToken = string.Empty;
                                if(!string.IsNullOrWhiteSpace(extToken))
                                {
                                    clientToken = extToken.Trim();
                                }
                                else
                                {
                                    clientToken = _clientTokenService.GenerateToken();
                                }

                                string tokenHash = ClientTokenHasher.Hash(clientToken);

                                bool saved = await _applicantRepository.SaveSecretAndTokenAsync(request.PortalId, mobileNo, deviceId, finalUserId, secretKey, clientToken, tokenHash);
                                if (!saved)
                                {
                                    _logger.LogWarning("Saving secret/token to database failed for mobile {Mobile}, portal {PortalId}, a_Id {AId}",
                                        mobileNo, request.PortalId, finalAId);
                                }

                                if (finalAId > 0)
                                {
                                    bool updated = await _applicantRepository.UpdateBeneficiaryVerificationStatusAsync(finalAId, "Y", deviceId);
                                    if (!updated)
                                    {
                                        _logger.LogWarning("OTP verified but updating tbl_beneficiary_details is_verified='Y' failed for a_id {AId}", finalAId);
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Updated tbl_beneficiary_details is_verified='Y' for a_id {AId}", finalAId);
                                    }
                                }

                                string portalName = !string.IsNullOrWhiteSpace(mapping.PortalName) ? mapping.PortalName : "eLabharthi";
                                string? otpauthUrl = null;
                                try
                                {
                                    otpauthUrl = TotpHelper.GenerateOtpauthUri(portalName, finalUserId, secretKey);
                                }
                                catch
                                {
                                    try
                                    {
                                        string base32Key = TotpHelper.GenerateBase32Secret(20);
                                        otpauthUrl = TotpHelper.GenerateOtpauthUri(portalName, finalUserId, base32Key);
                                    }
                                    catch { }
                                }

                                verifiedResults.Add(new VerifyOtpData
                                {
                                    A_Id = finalAId,
                                    BeneficiaryID = extBeneficiaryId,
                                    MobileNo = mobileNo,
                                    SecretKey = secretKey,
                                    Token = clientToken,
                                    VerifyDate = extVerifyDate ?? DateTime.Now,
                                    OtpauthUrl = otpauthUrl,
                                    PortalId = request.PortalId,
                                    UserId = finalUserId,
                                    IsVerified = "Y"
                                });
                            }
                        }
                    }
                    else if (rootStatus)
                    {
                        anySuccess = true;
                        foreach (long aid in aIdList)
                        {
                            string finalUserId = !string.IsNullOrWhiteSpace(userId) ? userId.Trim() : mobileNo.Trim();
                            string secretKey = TotpHelper.GenerateBase32Secret(20);
                            string clientToken = _clientTokenService.GenerateToken();
                            string tokenHash = ClientTokenHasher.Hash(clientToken);

                            await _applicantRepository.SaveSecretAndTokenAsync(request.PortalId, mobileNo, deviceId, finalUserId, secretKey, clientToken, tokenHash);
                            await _applicantRepository.UpdateBeneficiaryVerificationStatusAsync(aid, "Y", deviceId);

                            string portalName = !string.IsNullOrWhiteSpace(mapping.PortalName) ? mapping.PortalName : "eLabharthi";
                            string? otpauthUrl = null;
                            try { otpauthUrl = TotpHelper.GenerateOtpauthUri(portalName, finalUserId, secretKey); } catch { }

                            verifiedResults.Add(new VerifyOtpData
                            {
                                A_Id = aid,
                                MobileNo = mobileNo,
                                SecretKey = secretKey,
                                Token = clientToken,
                                VerifyDate = DateTime.Now,
                                OtpauthUrl = otpauthUrl,
                                PortalId = request.PortalId,
                                UserId = finalUserId,
                                IsVerified = "Y"
                            });
                        }
                    }
                    else
                    {
                        lastErrorMessage = message;
                        lastStatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    lastStatusCode = statusCode;
                    string errMessage = "Invalid or expired OTP.";
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        try
                        {
                            var token = JToken.Parse(responseBody);
                            if (token is JObject obj)
                            {
                                if (obj["message"] != null)
                                {
                                    errMessage = obj["message"]!.ToString();
                                }
                                else if (obj["errors"] is JObject errorsObj)
                                {
                                    var firstError = errorsObj.Properties().FirstOrDefault();
                                    if (firstError != null && firstError.Value is JArray errArr && errArr.Count > 0)
                                    {
                                        errMessage = errArr[0].ToString();
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    lastErrorMessage = errMessage;
                }

                // 7. Check if any verification succeeded
                if (!anySuccess || verifiedResults.Count == 0)
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = lastStatusCode >= 400 && lastStatusCode < 500 ? lastStatusCode : (int)HttpStatusCode.BadRequest,
                        Message = lastErrorMessage,
                        Data = null
                    };
                }

                // Mark OTP validated in local tbl_otp
                await _applicantRepository.MarkOtpValidatedAsync(mobileNo, otp);

                _logger.LogInformation("Successfully verified OTP for mobile {Mobile}, portal {PortalId}, count {Count}",
                    mobileNo, request.PortalId, verifiedResults.Count);

                // Return single object if 1 record verified, or array if multiple
                object dataPayload = verifiedResults.Count == 1 ? (object)verifiedResults[0] : verifiedResults;

                return new ApplicantResponse
                {
                    Status = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "OTP verified successfully.",
                    Data = dataPayload
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout connecting to external verify-otp for portal ID {PortalId}", request.PortalId);
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.GatewayTimeout,
                    Message = "Gateway Timeout: The external portal took too long to respond.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to external verify-otp for portal ID {PortalId}", request.PortalId);
                _exceptionLogging.SendErrorToText(ex);

                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Message = $"Bad Gateway: Unable to connect to external verify-otp API ({ex.Message})",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Builds the target external API URL combining the portal's base URL and the endpoint.
        /// </summary>
        private static string BuildTargetUrl(string? baseUrl, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = DefaultPortalUrl;
            }

            baseUrl = baseUrl.Trim().TrimEnd('/');

            // If the URL already ends with the target endpoint, use as is
            if (baseUrl.EndsWith(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

            // Strip any existing /api/Beneficiary suffix to avoid duplicate paths
            int apiIndex = baseUrl.IndexOf("/api/Beneficiary", StringComparison.OrdinalIgnoreCase);
            if (apiIndex >= 0)
            {
                baseUrl = baseUrl.Substring(0, apiIndex).TrimEnd('/');
            }

            return $"{baseUrl}/{endpoint.TrimStart('/')}";
        }

        private static string DecryptClientKey(string? clientKey)
        {
            if (string.IsNullOrWhiteSpace(clientKey))
                return string.Empty;

            try
            {
                return RSA_Enc_Dec.Decryption(clientKey);
            }
            catch
            {
                return clientKey;
            }
        }

        private static string DecryptField(string? text, string? clientKey)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(clientKey))
                return text;

            try
            {
                return Aes256CbcEncrypterApp.Decrypt(text, clientKey);
            }
            catch
            {
                return text;
            }
        }

        private static List<BeneficiaryData> ExtractBeneficiaries(string responseBody)
        {
            var list = new List<BeneficiaryData>();
            if (string.IsNullOrWhiteSpace(responseBody))
                return list;

            try
            {
                var token = JToken.Parse(responseBody);
                JArray? array = null;

                if (token is JObject obj)
                {
                    if (obj["data"] is JArray dataArr)
                    {
                        array = dataArr;
                    }
                    else if (obj["data"] is JObject dataObj)
                    {
                        var single = dataObj.ToObject<BeneficiaryData>();
                        if (single != null) list.Add(single);
                        return list;
                    }
                    else if (obj["BeneficiaryID"] != null || obj["beneficiaryID"] != null ||
                             obj["A_Id"] != null || obj["a_Id"] != null ||
                             obj["Name"] != null || obj["name"] != null)
                    {
                        var single = obj.ToObject<BeneficiaryData>();
                        if (single != null) list.Add(single);
                        return list;
                    }
                }
                else if (token is JArray rootArr)
                {
                    array = rootArr;
                }

                if (array != null)
                {
                    foreach (var item in array)
                    {
                        var itemDto = item.ToObject<BeneficiaryData>();
                        if (itemDto != null)
                        {
                            list.Add(itemDto);
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public async Task<ApplicantResponse> GetBeneficiaryMessagesAsync(GetBeneficiaryMessagesRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Token is required.",
                    Data = null
                };
            }

            string token = request.Token.Trim();
            long portalId = request.PortalId;
            string targetUrl = string.Empty;

            try
            {
                // 1. Resolve user, portal, mobile, and device information from the database using token
                var (resolvedPortalId, mobileNo, deviceId) = await _applicantRepository.GetUserByTokenAsync(token);
                portalId = resolvedPortalId;

                // 2. Resolve portal information (URL)
                var portal = await _portalRepository.GetPortalByIdAsync(portalId);
                string portalUrl = !string.IsNullOrWhiteSpace(portal?.PortalUrl) ? portal.PortalUrl : DefaultPortalUrl;
                string portalName = !string.IsNullOrWhiteSpace(portal?.PortalName) ? portal.PortalName : "eLabharthi";

                // 3. Construct target external API URL (http://10.133.20.151/eLabharthiAPI/api/Beneficiary/Get_Beneficiary_Messages)
                targetUrl = BuildTargetUrl(portalUrl, GetBeneficiaryMessagesEndpoint);
                if (string.IsNullOrWhiteSpace(targetUrl) || !Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
                {
                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = $"Invalid portal URL configured for portal '{portalName}': '{targetUrl}'",
                        Data = null
                    };
                }

                // 4. Call external eLabharthi API passing {"token": token}
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", Guid.NewGuid().ToString("N"));

                var forwardPayload = new { token = token };
                string jsonContent = JsonConvert.SerializeObject(forwardPayload);
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

                _logger.LogInformation("Calling external Beneficiary Messages API for portal '{PortalName}' at {TargetUrl} with token",
                    portalName, targetUri);

                using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
                string responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                int httpStatusCode = (int)httpResponse.StatusCode;

                _logger.LogInformation("External portal response received with status code {StatusCode}", httpStatusCode);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errMessage = $"External portal returned HTTP {httpStatusCode}.";
                    try
                    {
                        var tokenObj = JToken.Parse(responseBody);
                        if (tokenObj is JObject obj && obj["message"] != null)
                        {
                            errMessage = obj["message"]!.ToString();
                        }
                    }
                    catch { }

                    return new ApplicantResponse
                    {
                        Status = false,
                        StatusCode = httpStatusCode,
                        Message = errMessage,
                        Data = null
                    };
                }

                // 5. Extract beneficiary messages from response
                var messages = ExtractBeneficiaryMessages(responseBody);

                if (messages == null || messages.Count == 0)
                {
                    return new ApplicantResponse
                    {
                        Status = true,
                        StatusCode = (int)HttpStatusCode.OK,
                        Message = "No beneficiary messages found.",
                        Data = new List<SavedBeneficiaryMessageDto>()
                    };
                }

                // 6. Save each message in tbl_Message with mobile, and synced data in tbl_Message_log
                var savedList = new List<SavedBeneficiaryMessageDto>();

                foreach (var msg in messages)
                {
                    string itemMobile = !string.IsNullOrWhiteSpace(msg.MobNo) ? msg.MobNo : (mobileNo ?? string.Empty);
                    string? safeDeviceId = !string.IsNullOrWhiteSpace(deviceId) ? deviceId : (!string.IsNullOrWhiteSpace(itemMobile) ? itemMobile : "DEFAULT_DEVICE");

                    var (msgId, msgLogId) = await _applicantRepository.SaveBeneficiaryMessageAsync(portalId, itemMobile, safeDeviceId, msg);

                    savedList.Add(new SavedBeneficiaryMessageDto
                    {
                        MsgId = msgId,
                        MsgLogId = msgLogId,
                        PortalId = portalId,
                        DeviceId = safeDeviceId,
                        IsSynced = "Yes",
                        SyncDate = DateTime.Now,
                        Message_Id = msg.Message_Id,
                        Benificiary_No = msg.Benificiary_No,
                        MobNo = itemMobile,
                        Message_Head = msg.Message_Head,
                        Message_Body = msg.Message_Body,
                        Type = msg.Type,
                        Pic_Path1 = msg.Pic_Path1,
                        Pic_Path2 = msg.Pic_Path2,
                        Video_Path = msg.Video_Path,
                        Doc_Path = msg.Doc_Path,
                        CreatedBy = msg.CreatedBy,
                        CreatedDate = msg.CreatedDate
                    });
                }

                _logger.LogInformation("Successfully saved and synced {Count} beneficiary message(s) to tbl_Message and tbl_Message_log", savedList.Count);

                return new ApplicantResponse
                {
                    Status = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = $"Successfully retrieved and synced {savedList.Count} beneficiary message(s).",
                    Data = savedList
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout connecting to external portal ID {PortalId} at {TargetUrl}", portalId, targetUrl);
                _exceptionLogging.SendErrorToText(ex);

                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.GatewayTimeout,
                    Message = $"Gateway Timeout: The external portal at '{targetUrl}' took too long to respond (host may be offline or unreachable).",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get beneficiary messages for portal ID {PortalId} at {TargetUrl}", portalId, targetUrl);
                _exceptionLogging.SendErrorToText(ex);

                return new ApplicantResponse
                {
                    Status = false,
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Message = $"Bad Gateway: Unable to connect or process external messages API at '{targetUrl}' ({ex.Message})",
                    Data = null
                };
            }
        }

        private static List<BeneficiaryMessageItem> ExtractBeneficiaryMessages(string responseBody)
        {
            var list = new List<BeneficiaryMessageItem>();
            if (string.IsNullOrWhiteSpace(responseBody)) return list;

            try
            {
                var token = JToken.Parse(responseBody);
                JArray? array = null;

                if (token is JObject obj)
                {
                    if (obj["data"] is JArray dataArr)
                    {
                        array = dataArr;
                    }
                    else if (obj["Data"] is JArray dataArrUpper)
                    {
                        array = dataArrUpper;
                    }
                    else if (obj["data"] is JObject dataObj)
                    {
                        var single = dataObj.ToObject<BeneficiaryMessageItem>();
                        if (single != null) list.Add(single);
                        return list;
                    }
                    else if (obj["message_Id"] != null || obj["Message_Id"] != null ||
                             obj["benificiary_No"] != null || obj["mobNo"] != null)
                    {
                        var single = obj.ToObject<BeneficiaryMessageItem>();
                        if (single != null) list.Add(single);
                        return list;
                    }
                }
                else if (token is JArray rootArr)
                {
                    array = rootArr;
                }

                if (array != null)
                {
                    foreach (var item in array)
                    {
                        var itemDto = item.ToObject<BeneficiaryMessageItem>();
                        if (itemDto != null)
                        {
                            list.Add(itemDto);
                        }
                    }
                }
            }
            catch { }

            return list;
        }
    }
}
