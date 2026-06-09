namespace LicensingAPI.Models.Audit
{
    public class AuditLogDTO
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Changes { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
    }
}
