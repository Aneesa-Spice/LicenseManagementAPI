using System.Text.Json.Serialization;

namespace LicensingAPI.Models
{
    public class PagedResult<T>
    {
        public T Data { get; set; }
        public KeygenMeta? Meta { get; set; }
    }

    public class APIResponse<T>
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<ValidationError>? Errors { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationMetadata? Pagination { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string? SerialKey { get; set; }
        public string? KeygenId { get; set; }
        public IList<string>? Roles { get; set; }
    }

    public class PaginationMetadata
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
