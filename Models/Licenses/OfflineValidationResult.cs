using System.Text.Json.Serialization;

namespace LicensingAPI.Models.Licenses
{
    public class OfflineValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public LicensePayload Payload { get; set; }

        public static OfflineValidationResult Success(LicensePayload payload) =>
            new() { IsValid = true, Message = "License is valid.", Payload = payload };

        public static OfflineValidationResult Fail(string message) =>
            new() { IsValid = false, Message = message };
    }

    public class LicensePayload
    {
        [JsonPropertyName("id")]
        public string LicenseId { get; set; }

        [JsonPropertyName("exp")]
        public long? ExpiryUnix { get; set; }           // Unix timestamp from Keygen

        [JsonPropertyName("pol")]
        public string PolicyId { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        // Computed
        public DateTime? Expiry => ExpiryUnix.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(ExpiryUnix.Value).UtcDateTime
            : null;
    }
}
