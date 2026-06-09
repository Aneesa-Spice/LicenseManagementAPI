using LicensingAPI.Models;
using LicensingAPI.Models.Licenses;
using LicensingAPI.Models.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LicensingAPI.Services
{
    public class KeygenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeygenService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public KeygenService(HttpClient httpClient,
            IConfiguration configuration,
            ILogger<KeygenService> logger,
            IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
        }

        public async Task<(string BaseUrl, string ApiToken, string AccountId,string PublicKey)> GetSettingsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LicensingAPI.Data.ApplicationDbContext>();

            var baseUrl = await context.AppSettings
                .Where(a => a.Key == "BASE_URL")
                .Select(a => a.Value)
                .FirstOrDefaultAsync() ?? "https://api.keygen.sh";

            var apiToken = await context.AppSettings
                .Where(a => a.Key == "API_TOKEN")
                .Select(a => a.Value)
                .FirstOrDefaultAsync() ?? string.Empty;

            var accountId = await context.AppSettings
                .Where(a => a.Key == "ACCOUNT_ID")
                .Select(a => a.Value)
                .FirstOrDefaultAsync() ?? string.Empty;

            var publickey = await context.AppSettings
                .Where(a => a.Key == "PUBLIC_KEY")
                .Select(a => a.Value)
                .FirstOrDefaultAsync() ?? string.Empty;

            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            return (baseUrl, apiToken, accountId,publickey);
        }


        public async Task<KeygenLicenseResult> CreateLicenseAsync(KeygenLicenseRequest request)
        {
            var result = new KeygenLicenseResult();
            var settings = await GetSettingsAsync();

            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "licenses",
                        attributes = new
                        {
                            name = request.Name,
                            expiry = request.createLicense.Expiry,   // ✅ add expiry
                            maxMachines = request.createLicense.MaxMachines,  // ✅ add machine count
                            maxUsers = request.createLicense.MaxUsers      // ✅ add user count
                        },
                        relationships = new
                        {
                            policy = new { data = new { type = "policies", id = request.PolicyId } }
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/licenses";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrEmpty(settings.ApiToken) ? request.APIKey : settings.ApiToken);
                req.Headers.Add("Accept", "application/vnd.api+json");
                req.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/vnd.api+json"
                );

                var response = await _httpClient.SendAsync(req);
                var body = await response.Content.ReadAsStringAsync();
                result.RawResponse = body;
                result.StatusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen returned {Status}: {Body}", (int)response.StatusCode, body);
                    result.Success = false;
                    return result;
                }

                var deserialized = JsonSerializer.Deserialize<KeygenLicenseResult>(body);
                if (deserialized != null)
                {
                    result.Data = deserialized.Data;
                    result.Errors = deserialized.Errors;
                    result.Success = true;

                    if (result.Data?.Attributes != null)
                    {
                        result.LicenseId = result.Data.Id;
                        result.LicenseKey = result.Data.Attributes.Key;
                        result.Expiry = result.Data.Attributes.Expiry;
                        result.IsTrial = result.Data.Attributes.IsTrial;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception calling Keygen API for {Email}", request.UserEmail);
                result.Success = false;
            }

            return result;
        }

        public async Task<KeygenLicenseResult?> GetLicenseByIdAsync(string licenseId)
        {
            var result = new KeygenLicenseResult();
            var settings = await GetSettingsAsync();

            var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/licenses/{licenseId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiToken);
            request.Headers.Add("Accept", "application/vnd.api+json");

            var response = await _httpClient.SendAsync(request);

            var rawJson = await response.Content.ReadAsStringAsync();

            result.StatusCode = (int)response.StatusCode;
            result.RawResponse = rawJson;

            if (!response.IsSuccessStatusCode)
            {
                result.Success = false;
                var errorResponse = JsonSerializer.Deserialize<KeygenResponse<object>>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                result.Errors = errorResponse?.Errors;
                return result;
            }

            var deserialized = JsonSerializer.Deserialize<KeygenResponse<KeygenData<KeygenLicenseAttributes>>>(
                rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (deserialized?.Data != null)
            {
                var val = deserialized.Data.Attributes;
                result.Success = true;
                result.Data = deserialized.Data;       // ✅ full data object
                result.LicenseId = deserialized.Data.Id;    // Keygen UUID
                result.LicenseKey = val.Key;               // license key string
                result.Expiry = val.Expiry;            // nullable datetime
                result.IsTrial = val.IsTrial;           // directly from your attribute
                result.Errors = deserialized.Errors;
                result.Meta = deserialized.Meta;
            }
            return result;
        }
        public async Task<bool> DeleteLicenseAsync(string accountId,string licenseKey)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var url = $"{settings.BaseUrl}accounts/{accountId}/licenses/{licenseKey}";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("License deleted successfully in Keygen.");
                    return true;
                }

                _logger.LogWarning("Failed to delete license in Keygen. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for Delete.");
                return false;
            }
        }

        public async Task<bool> UpdateLicenseExpiryAsync(string keygenId, UpdateLicenseRequest request)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "licenses",
                        attributes = new
                        {
                            expiry = request.Expiry?.ToString("O"),
                            maxMachines = request.MaxMachines,
                            maxUsers = request.MaxUsers
                        }
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");
                var url = $"{settings.BaseUrl}accounts/{accountId}/licenses/{keygenId}";

                using var req = new HttpRequestMessage(HttpMethod.Put, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = jsonContent;

                var response = await _httpClient.SendAsync(req);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogWarning("Failed to update license in Keygen. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for Update.");
                return false;
            }
        }

        public async Task<bool> RevokeLicenseAsync(string accountId, string licenseKey)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "licenses",
                        attributes = new
                        {
                            status = "SUSPENDED"
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/licenses/{licenseKey}";
                using var req = new HttpRequestMessage(HttpMethod.Patch, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Exception revoking license in Keygen.");
                return false;
            }
        }

        public async Task<(bool Success, string? MachineId, string? Message)> ActivateMachineAsync(string licenseId, string fingerprint, string? name, string? platform)
        {
            var settings = await GetSettingsAsync();
            return await ActivateMachineAsync(settings.AccountId, licenseId, fingerprint, name, platform);
        }

        public async Task<(bool Success, string? MachineId, string? Message)> ActivateMachineAsync(string accountId, string licenseId, string fingerprint, string? name, string? platform)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "machines",
                        attributes = new
                        {
                            fingerprint = fingerprint,
                            name = name,
                            platform = platform
                        },
                        relationships = new
                        {
                            license = new { data = new { type = "licenses", id = licenseId } }
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/machines";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(body);
                    var machineId = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
                    return (true, machineId, "Machine activated successfully");
                }

                _logger.LogError("Keygen Machine Activation Error: {Status} - {Body}", response.StatusCode, body);
                return (false, null, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen ActivateMachine");
                return (false, null, ex.Message);
            }
        }

        public async Task<bool> DeactivateMachineAsync(string machineId)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/machines/{machineId}";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen DeactivateMachine");
                return false;
            }
        }


        public async Task<bool> ValidateLicenseAsync(string licenseKey)
        {
            var result = await ValidateLicenseDetailsAsync(licenseKey);
            return result.Success;
        }

        public async Task<KeygenLicenseResult> ValidateLicenseDetailsAsync(string licenseKey, string? fingerprint = null)
        {
            var result = new KeygenLicenseResult();
            var settings = await GetSettingsAsync();

            try
            {
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/licenses/actions/validate-key";

                var body = new
                {
                    meta = new
                    {
                        key = licenseKey,
                        scope = string.IsNullOrEmpty(fingerprint) ? null : new { fingerprint = fingerprint }
                    }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("Accept", "application/vnd.api+json");
                req.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();
                result.RawResponse = responseBody;
                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var deserialized = JsonSerializer.Deserialize<KeygenLicenseResult>(responseBody, options);
                    
                    if (deserialized != null)
                    {
                        result.Data = deserialized.Data;
                        result.Meta = deserialized.Meta;
                        result.Errors = deserialized.Errors;

                        // Keygen validation response has a 'valid' boolean in meta
                        using var doc = JsonDocument.Parse(responseBody);
                        bool isValid = false;
                        if (doc.RootElement.TryGetProperty("meta", out var meta))
                        {
                            if (meta.TryGetProperty("valid", out var validProp))
                            {
                                isValid = validProp.GetBoolean();
                            }
                        }
                        result.Success = isValid;

                        if (result.Data?.Attributes != null)
                        {
                            result.LicenseId = result.Data.Id;
                            result.LicenseKey = result.Data.Attributes.Key;
                            result.Expiry = result.Data.Attributes.Expiry;
                            result.IsTrial = result.Data.Attributes.IsTrial;
                        }
                    }
                }
                else
                {
                    result.Success = false;
                    _logger.LogWarning("Keygen validation failed with status {Status}: {Body}", response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen ValidateLicenseDetails");
                result.Success = false;
            }

            return result;
        }


        // --- GET LIST FUNCTIONS ---
        #region
        public async Task<(List<KeygenLicenseDto> Data, KeygenMeta? meta)> GetLicensesAsync(
        int pageNumber, int pageSize, string? searchKey = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                // Build URL with optional search filter - Encoding [ and ] as %5B and %5D
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/licenses?page%5Bnumber%5D={pageNumber}&page%5Bsize%5D={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchKey))
                    url += $"&filter%5Bkey%5D={Uri.EscapeDataString(searchKey)}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetLicenses): {StatusCode} - {Body}",
                        response.StatusCode, responseBody);
                    return (new List<KeygenLicenseDto>(), null);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenLicenseAttributes>>>>(responseBody, options);
                var data = result?.Data?.Select(d => new KeygenLicenseDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Key = d.Attributes.Key,
                    Expiry = d.Attributes.Expiry,
                    Status = d.Attributes.Status,
                    CreatedAt = d.Attributes.CreatedAt
                }).ToList() ?? new List<KeygenLicenseDto>();

                KeygenMeta keygenMeta = new KeygenMeta()
                {
                    //Page =result.,
                    Pages = result?.Links?.Meta?.Pages ?? 0,
                    Count = result.Links.Meta.Count,
                };

                //return (data, result?.Meta);
                return (data, keygenMeta);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetLicenses.");
                return (new List<KeygenLicenseDto>(), null);
            }
        }

        public async Task<(List<KeygenProductDto> Data, KeygenMeta? Meta)> GetProductsAsync(
        int pageNumber, int pageSize, string? searchKey = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/products?page%5Bnumber%5D={pageNumber}&page%5Bsize%5D={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchKey))
                    url += $"&filter%5Bcode%5D={Uri.EscapeDataString(searchKey)}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetProducts): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return (new List<KeygenProductDto>(), null);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenProductAttributes>>>>(responseBody, options);

                var data = result?.Data.Select(d => new KeygenProductDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Code = d.Attributes.Code,
                    Url = d.Attributes.Url,
                    Description = d.Attributes.Description
                }).ToList() ?? new List<KeygenProductDto>();

                KeygenMeta keygenMeta = new KeygenMeta()
                {
                    //Page =result.,
                    Pages = result?.Links?.Meta?.Pages ?? 0,
                    Count = result.Links.Meta.Count,
                };

                return (data, keygenMeta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetProducts.");
                return (new List<KeygenProductDto>(), null);
            }
        }

        public async Task<(List<KeygenPolicyDto> Data, KeygenMeta? Meta)> GetPoliciesAsync(
        int pageNumber, int pageSize, string? searchKey = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/policies?page%5Bnumber%5D={pageNumber}&page%5Bsize%5D={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchKey))
                    url += $"&filter%5Bname%5D={Uri.EscapeDataString(searchKey)}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetPolicies): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return (new List<KeygenPolicyDto>(), null);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenPolicyAttributes>>>>(responseBody, options);

                var data = result?.Data.Select(d => new KeygenPolicyDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Duration = d.Attributes.Duration,
                    Created = d.Attributes.Created
                }).ToList() ?? new List<KeygenPolicyDto>();

                KeygenMeta keygenMeta = new KeygenMeta()
                {
                    //Page =result.,
                    Pages = result?.Links?.Meta?.Pages ?? 0,
                    Count = result.Links.Meta.Count,
                };

                return (data, keygenMeta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetPolicies.");
                return (new List<KeygenPolicyDto>(), null);
            }
        }
        #endregion


        // --- PRODUCT CRUD ---
        #region
        public async Task<string?> CreateProductAsync(string name, string code, string description)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "products",
                        attributes = new
                        {
                            name = name,
                            code = code,
                            metadata = new { description }
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/products";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen CreateProduct Error: {Status} - {Body}", response.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen CreateProduct");
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(string keygenId, string name, string code, string description)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "products",
                        attributes = new
                        {
                            name = name,
                            code = code,
                            metadata = new { description }
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/products/{keygenId}";
                using var req = new HttpRequestMessage(HttpMethod.Patch, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Keygen UpdateProduct Error: {Status} - {Body}", response.StatusCode, body);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen UpdateProduct");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string keygenId)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var url = $"{settings.BaseUrl}accounts/{accountId}/products/{keygenId}";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen DeleteProduct");
                return false;
            }
        }
        #endregion


        // --- POLICY CRUD ---
        #region
        public async Task<string?> CreatePolicyAsync(CreatePolicyRequest request, string keygenProductId)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "policies",
                        attributes = new
                        {
                            name = request.Name,
                            duration = request.Duration,       // null = never expires
                            strict = request.Strict,
                            floating = request.Floating,
                            //scheme = request.SchemeCode,         // null = online, ED25519_SIGN = offline
                            maxMachines = request.MaxMachines,
                            expirationStrategy = request.ExpirationStrategy
                        },
                        relationships = new
                        {
                            product = new
                            {
                                data = new { type = "products", id = keygenProductId }
                            }
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/policies";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen CreatePolicy Error: {Status} - {Body}", response.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen CreatePolicy");
                return null;
            }
        }

        public async Task<bool> UpdatePolicyAsync(string keygenId, string name, int duration)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new
                    {
                        type = "policies",
                        attributes = new
                        {
                            name = name,
                            duration = duration
                        }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/policies/{keygenId}";
                using var req = new HttpRequestMessage(HttpMethod.Patch, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Keygen UpdatePolicy Error: {Status} - {Body}", response.StatusCode, body);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen UpdatePolicy");
                return false;
            }
        }

        public async Task<bool> DeletePolicyAsync(string keygenId)
        {
            var accountId = _configuration["Keygen:AccountId"];
            var settings = await GetSettingsAsync();
            try
            {
                var url = $"{settings.BaseUrl}accounts/{accountId}/policies/{keygenId}";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen DeletePolicy");
                return false;
            }
        }

        public async Task<KeygenPolicyResult?> GetPolicyByIdAsync(string policyId)
        {
            var result = new KeygenPolicyResult();
            var settings = await GetSettingsAsync();

            var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/policies/{policyId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiToken);
            request.Headers.Add("Accept", "application/vnd.api+json");

            var response = await _httpClient.SendAsync(request);
            var rawJson = await response.Content.ReadAsStringAsync();

            result.StatusCode = (int)response.StatusCode;
            result.RawResponse = rawJson;

            if (!response.IsSuccessStatusCode)
            {
                result.Success = false;
                var errorResponse = JsonSerializer.Deserialize<KeygenResponse<object>>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                result.Errors = errorResponse?.Errors;
                return result;
            }

            var deserialized = JsonSerializer.Deserialize<KeygenResponse<KeygenData<KeygenPolicyAttributes>>>(
                rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (deserialized?.Data != null)
            {
                var attrs = deserialized.Data.Attributes;
                result.Success = true;
                result.Data = deserialized.Data;
                result.Errors = deserialized.Errors;
                result.Meta = deserialized.Meta;
            }

            return result;
        }

        #endregion


        // --- DROPDOWN FUNCTIONS
        #region

        public async Task<List<KeygenPolicyDto>> GetPoliciesAsync()
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/policies";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetPolicies): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new List<KeygenPolicyDto>();
                }

                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenPolicyAttributes>>>>(responseBody);

                return result?.Data.Select(d => new KeygenPolicyDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Duration = d.Attributes.Duration,
                    Created = d.Attributes.Created
                }).ToList() ?? new List<KeygenPolicyDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetPolicies.");
                return new List<KeygenPolicyDto>();
            }
        }

        public async Task<List<KeygenProductDto>> GetProductsAsync()
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/products";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetProducts): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new List<KeygenProductDto>();
                }

                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenProductAttributes>>>>(responseBody);

                return result?.Data.Select(d => new KeygenProductDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Code = d.Attributes.Code,
                    Url = d.Attributes.Url,
                    Description = d.Attributes.Description
                }).ToList() ?? new List<KeygenProductDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetProducts.");
                return new List<KeygenProductDto>();
            }
        }

        public async Task<(List<LicensingAPI.Models.Activations.MachineDto> Data, KeygenMeta? meta)> GetMachinesAsync(
            int pageNumber, int pageSize, string? searchKey = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/machines?page%5Bnumber%5D={pageNumber}&page%5Bsize%5D={pageSize}";

                if (!string.IsNullOrWhiteSpace(searchKey))
                    url += $"&filter%5Bfingerprint%5D={Uri.EscapeDataString(searchKey)}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetMachines): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return (new List<LicensingAPI.Models.Activations.MachineDto>(), null);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KeygenResponse<List<KeygenData<KeygenMachineAttributes>>>>(responseBody, options);
                
                var data = result?.Data?.Select(d => new LicensingAPI.Models.Activations.MachineDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Fingerprint = d.Attributes.Fingerprint,
                    Cores = d.Attributes.Cores ?? 0,
                    Ip = d.Attributes.Ip,
                    Hostname = d.Attributes.Hostname,
                    Platform = d.Attributes.Platform,
                    CreatedAt = d.Attributes.Created,
                    UpdatedAt = d.Attributes.Updated,
                    LicenseId = d.Relationships?.Policy?.Data?.Id // Actually relationships.license.data.id, but we'll leave it empty for now or parse it later
                }).ToList() ?? new List<LicensingAPI.Models.Activations.MachineDto>();

                KeygenMeta keygenMeta = new KeygenMeta()
                {
                    Pages = result?.Links?.Meta?.Pages ?? 0,
                    Count = result?.Links?.Meta?.Count ?? 0,
                };

                return (data, keygenMeta);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetMachines.");
                return (new List<LicensingAPI.Models.Activations.MachineDto>(), null);
            }
        }

        public async Task<LicensingAPI.Models.Activations.MachineDto?> GetMachineAsync(string machineId)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var url = $"{settings.BaseUrl}accounts/{settings.AccountId}/machines/{machineId}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

                var response = await _httpClient.SendAsync(req);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keygen API Error (GetMachine): {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KeygenResponse<KeygenData<KeygenMachineAttributes>>>(responseBody, options);

                if (result?.Data == null) return null;

                var d = result.Data;
                return new LicensingAPI.Models.Activations.MachineDto
                {
                    Id = d.Id,
                    Name = d.Attributes.Name,
                    Fingerprint = d.Attributes.Fingerprint,
                    Cores = d.Attributes.Cores ?? 0,
                    Ip = d.Attributes.Ip,
                    Hostname = d.Attributes.Hostname,
                    Platform = d.Attributes.Platform,
                    CreatedAt = d.Attributes.Created,
                    UpdatedAt = d.Attributes.Updated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception communicating with Keygen API for GetMachine.");
                return null;
            }
        }

        #endregion

        #region USER & MAPPING
        public async Task<string?> GetOrCreateUserAsync(string accountId, string email, string fullName)
        {
            var settings = await GetSettingsAsync();
            try
            {
                // 1. Try to find user by email
                var searchUrl = $"{settings.BaseUrl}accounts/{accountId}/users?filter[email]={Uri.EscapeDataString(email)}";
                using var searchReq = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                searchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                
                var searchResponse = await _httpClient.SendAsync(searchReq);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchBody = await searchResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(searchBody);
                    var data = doc.RootElement.GetProperty("data");
                    if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                    {
                        return data[0].GetProperty("id").GetString();
                    }
                }

                // 2. Not found, create user
                var payload = new
                {
                    data = new
                    {
                        type = "users",
                        attributes = new
                        {
                            email = email,
                            firstName = fullName
                        }
                    }
                };

                var createUrl = $"{settings.BaseUrl}accounts/{accountId}/users";
                using var createReq = new HttpRequestMessage(HttpMethod.Post, createUrl);
                createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                createReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var createResponse = await _httpClient.SendAsync(createReq);
                var createBody = await createResponse.Content.ReadAsStringAsync();
                
                if (createResponse.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(createBody);
                    return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
                }

                _logger.LogError("Failed to create user in Keygen: {Body}", createBody);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen GetOrCreateUser");
                return null;
            }
        }

        public async Task<bool> AttachUserToLicenseAsync(string accountId, string licenseId, string userId)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new[]
                    {
                        new { type = "users", id = userId }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/licenses/{licenseId}/relationships/users";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);
                if (response.IsSuccessStatusCode) return true;

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to attach user to license in Keygen: {Status} - {Body}", response.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen AttachUserToLicense");
                return false;
            }
        }

        public async Task<bool> DetachUserFromLicenseAsync(string accountId, string licenseId, string userId)
        {
            var settings = await GetSettingsAsync();
            try
            {
                var payload = new
                {
                    data = new[]
                    {
                        new { type = "users", id = userId }
                    }
                };

                var url = $"{settings.BaseUrl}accounts/{accountId}/licenses/{licenseId}/relationships/users";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/vnd.api+json");

                var response = await _httpClient.SendAsync(req);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Keygen DetachUserFromLicense");
                return false;
            }
        }
        #endregion
    }
}
