using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class ApiLog : BaseEntity
{
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? LicenseId { get; set; }
    public Guid? UserId { get; set; }
}
