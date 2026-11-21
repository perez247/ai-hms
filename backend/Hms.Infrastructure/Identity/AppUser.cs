using Hms.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Hms.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        Id = Guid.CreateVersion7();
    }

    public string FullName { get; set; } = string.Empty;
    public StaffRole StaffRole { get; set; }
    public string? EmployeeNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? SalaryAmount { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public DateOnly? HiredOn { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
