using LicensingAPI.Models.Licenses;
using System.Text.Json.Serialization;

namespace LicensingAPI.Models
{
    public class KeygenResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("links")]
        public KeygenLinks? Links { get; set; }

        [JsonPropertyName("meta")]
        public KeygenMeta? Meta { get; set; }

        [JsonPropertyName("errors")]
        public List<KeygenError>? Errors { get; set; }
    }

    public class KeygenLicenseRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? Name { get; set; } = string.Empty;
        public string PolicyId { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;
        public string BaseURL { get; set; } = string.Empty;
        public CreateLicenseRequest createLicense = new CreateLicenseRequest();
    }

    public class KeygenLicenseResult : KeygenResponse<KeygenData<KeygenLicenseAttributes>>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string? LicenseId { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? Expiry { get; set; }
        public bool IsTrial { get; set; }
        public string? RawResponse { get; set; }
        public bool HasErrors => Errors != null && Errors.Count > 0;
    }

    public class KeygenLinks
    {
        [JsonPropertyName("self")]
        public string? Self { get; set; }

        [JsonPropertyName("first")]
        public string? First { get; set; }

        [JsonPropertyName("last")]
        public string? Last { get; set; }

        [JsonPropertyName("prev")]
        public string? Prev { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("meta")]
        public KeygenMeta? Meta { get; set; }
    }

    public class KeygenMeta
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("pages")]
        public int? Pages { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

    }

    public class KeygenData<T>
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public T Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public KeygenLicenseRelationships? Relationships { get; set; }
    }

    public class KeygenLicenseRelationships
    {
        [JsonPropertyName("policy")]
        public KeygenRelationship? Policy { get; set; }

        [JsonPropertyName("product")]
        public KeygenRelationship? Product { get; set; }

        [JsonPropertyName("owner")]
        public KeygenRelationship? Owner { get; set; }

        [JsonPropertyName("group")]
        public KeygenRelationship? Group { get; set; }
    }

    public class KeygenRelationship
    {
        [JsonPropertyName("data")]
        public KeygenRelationshipData? Data { get; set; }
    }

    public class KeygenRelationshipData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class KeygenProductAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }
    }



    public class KeygenProductDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
    }

    public class KeygenPolicyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int? Duration { get; set; }
        public DateTime Created { get; set; }
    }

    public class KeygenLicenseAttributes
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("expiry")]
        public DateTime? Expiry { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("uses")]
        public int? Uses { get; set; }

        [JsonPropertyName("maxUses")]
        public int? MaxUses { get; set; }

        [JsonPropertyName("maxMachines")]
        public int? MaxMachines { get; set; } //machine count

        [JsonPropertyName("maxProcesses")]
        public int? MaxProcesses { get; set; }

        [JsonPropertyName("maxCores")]
        public int? MaxCores { get; set; }

        [JsonPropertyName("maxUsers")]
        public int? MaxUsers { get; set; } //user count

        [JsonPropertyName("floating")]
        public bool? Floating { get; set; }

        [JsonPropertyName("protected")]
        public bool? Protected { get; set; }

        [JsonPropertyName("suspended")]
        public bool? Suspended { get; set; }

        [JsonPropertyName("isTrial")]
        public bool IsTrial { get; set; }

        [JsonPropertyName("scheme")]
        public string? Scheme { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("created")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated")]
        public DateTime UpdatedAt { get; set; }
    }

    public class KeygenLicenseDto
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string Key { get; set; }
        public DateTime? Expiry { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? PolicyId { get; set; }
        public string? PolicyName { get; set; }
        public string? UserEmail { get; set; }
    }

    public class KeygenMachineAttributes
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; } = string.Empty;

        [JsonPropertyName("cores")]
        public int? Cores { get; set; }

        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }
    }

    public class KeygenError
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("source")]
        public KeygenErrorSource? Source { get; set; }
    }

    public class KeygenErrorSource
    {
        [JsonPropertyName("pointer")]
        public string? Pointer { get; set; }

        [JsonPropertyName("parameter")]
        public string? Parameter { get; set; }
    }

    public class ClientLicenseResponse
    {
        public bool IsValid { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

