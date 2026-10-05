using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Services
{
    /// <summary>
    /// Service to dynamically forward requests to external portal APIs using configurations
    /// from mst_portal and user mapping credentials from tbl_map_user_portal.
    /// </summary>
    public class PortalService
    {
        private readonly IPortalRepository _portalRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PortalService> _logger;

        public PortalService(
            IPortalRepository portalRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<PortalService> logger)
        {
            _portalRepository = portalRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<PortalConnectResponse> ConnectAsync(
            PortalConnectRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.PortalId <= 0)
            {
                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 400,
                    Message = "Valid portal_id is required."
                };
            }

            // 1. Query mst_portal and tbl_map_user_portal
            var mapping = await _portalRepository.GetPortalAndUserMappingAsync(request.PortalId, request.Mobile);
            if (mapping == null)
            {
                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 404,
                    PortalId = request.PortalId,
                    Message = $"Portal ID {request.PortalId} was not found in mst_portal."
                };
            }

            // 2. Validate portal active state
            if (!string.IsNullOrWhiteSpace(mapping.IsActive) &&
                (mapping.IsActive.Equals("N", StringComparison.OrdinalIgnoreCase) || mapping.IsActive == "0"))
            {
                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 400,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    Message = $"Portal '{mapping.PortalName}' (ID {mapping.PortalId}) is currently marked inactive in mst_portal."
                };
            }

            // 3. Validate portal URL
            if (string.IsNullOrWhiteSpace(mapping.PortalUrl))
            {
                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 400,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    Message = $"Portal '{mapping.PortalName}' (ID {mapping.PortalId}) does not have a configured portal_url."
                };
            }

            // 4. Construct target URI
            string targetUrl = mapping.PortalUrl.Trim();
            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
            {
                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 400,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    TargetUrl = targetUrl,
                    Message = $"Invalid portal URL configured in database: '{targetUrl}'"
                };
            }

            // 5. Build HTTP request
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUri);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Inject credentials from tbl_map_user_portal
            if (!string.IsNullOrWhiteSpace(mapping.Token))
            {
                httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {mapping.Token.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(mapping.SecretKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Secret-Key", mapping.SecretKey.Trim());
            }

            if (!string.IsNullOrWhiteSpace(request.ClientKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-Client-Key", request.ClientKey.Trim());
            }

            // Forward clean payload with portalId, mobile, session, deviceId, and mapped credentials
            var forwardPayload = new Dictionary<string, object?>
            {
                ["portal_id"] = mapping.PortalId,
                ["mobile"] = string.IsNullOrWhiteSpace(request.Mobile) ? mapping.Mobile : request.Mobile.Trim(),
                ["session"] = request.Session?.Trim(),
                ["device_id"] = request.DeviceId?.Trim(),
                ["user_id"] = mapping.UserId,
                ["token"] = mapping.Token,
                ["secret_key"] = mapping.SecretKey
            };

            string jsonBody = JsonConvert.SerializeObject(forwardPayload);
            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 6. Execute external portal HTTP call
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                _logger.LogInformation("Forwarding request to external portal '{PortalName}' (ID {PortalId}) at {TargetUrl}",
                    mapping.PortalName, mapping.PortalId, targetUri);

                using var response = await client.SendAsync(httpRequest, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                object? parsedData = null;
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        parsedData = JsonConvert.DeserializeObject(responseBody);
                    }
                    catch
                    {
                        parsedData = responseBody;
                    }
                }

                int statusCode = (int)response.StatusCode;
                bool isSuccess = response.IsSuccessStatusCode;

                return new PortalConnectResponse
                {
                    Status = isSuccess,
                    StatusCode = statusCode,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    TargetUrl = targetUri.ToString(),
                    Message = isSuccess ? "External portal response received." : $"External portal returned HTTP {statusCode}.",
                    Data = parsedData
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout connecting to external portal '{PortalName}' at {TargetUrl}",
                    mapping.PortalName, targetUri);

                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 504,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    TargetUrl = targetUri.ToString(),
                    Message = "Gateway Timeout: external portal took too long to respond."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to external portal '{PortalName}' at {TargetUrl}",
                    mapping.PortalName, targetUri);

                return new PortalConnectResponse
                {
                    Status = false,
                    StatusCode = 502,
                    PortalId = mapping.PortalId,
                    PortalName = mapping.PortalName,
                    TargetUrl = targetUri.ToString(),
                    Message = $"Bad Gateway: Unable to connect to external portal ({ex.Message})"
                };
            }
        }
    }
}
