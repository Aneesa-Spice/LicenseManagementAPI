using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LicensingAPI.Models.Policies
{
    public class KeygenPolicyResult : KeygenResponse<KeygenData<KeygenPolicyAttributes>>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string? RawResponse { get; set; }
        public bool HasErrors => Errors != null && Errors.Count > 0;
    }

    public class KeygenPolicyAttributes
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }          // in seconds

        [JsonPropertyName("floating")]
        public bool? Floating { get; set; }         // ✅ key for maxMachines check

        [JsonPropertyName("strict")]
        public bool? Strict { get; set; }

        [JsonPropertyName("maxMachines")]
        public int? MaxMachines { get; set; }       // ✅ policy default

        [JsonPropertyName("maxUsers")]
        public int? MaxUsers { get; set; }          // ✅ policy default

        [JsonPropertyName("maxCores")]
        public int? MaxCores { get; set; }

        [JsonPropertyName("requireHeartbeat")]
        public bool? RequireHeartbeat { get; set; }

        [JsonPropertyName("requireCheckIn")]
        public bool? RequireCheckIn { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }
    }

    public class CreatePolicyRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? MaxMachines { get; set; }
        public int? Duration { get; set; }        // In seconds (e.g. 31556952 = 1 year)
        //public DateTime? DurationDate { get; set; } = null;
        public bool Floating { get; set; } = true;       // true = multiple machines
        public bool Strict { get; set; } = false;
       // public string SchemeCode { get; set; } = string.Empty;   // e.g. "ED25519_SIGN"
        public string ExpirationStrategy { get; set; } = string.Empty;// e.g. "RESTRICT_ACCESS"
        [Required]
        public string ProductId { get; set; } = string.Empty;

        // ✅ Dropdown lists
        public List<SelectListItem>? SchemeOptions { get; set; }
        public List<SelectListItem>? ExpirationStrategyOptions { get; set; }
        public List<SelectListItem>? DurationOptions { get; set; }
        public SelectList? ProductsList { get; set; }
        // //[Required]
        // //[MaxLength(100)]
        // public string Name { get; set; } = string.Empty;
        // public int? MaxMachines { get; set; }
        // public int? Duration { get; set; }        // In seconds (e.g. 31556952 = 1 year)
        //// public DateTime? DurationDate { get; set; } = null;
        // public bool Floating { get; set; } = true;     // true = multiple machines
        // public bool Strict { get; set; } = false;
        // public string SchemeCode { get; set; } = string.Empty;   // e.g. "ED25519_SIGN"
        // public string ExpirationStrategy { get; set; } = string.Empty;// e.g. "RESTRICT_ACCESS"
        // [Required]
        // public string ProductId { get; set; } = string.Empty;
    }

    public class UpdatePolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Duration { get; set; }
    }

    public class PolicyDTO
    {
        public string Id { get; set; }
        public int LocalId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
