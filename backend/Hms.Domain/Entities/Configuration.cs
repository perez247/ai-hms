using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class ClinicSetting
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
}

public class FeeConfiguration : Entity
{
    public FeeCode Code { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
