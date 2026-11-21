using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class HmoCompany : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ContractTerms { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<HmoContract> Contracts { get; set; } = new List<HmoContract>();
    public ICollection<HmoTariff> Tariffs { get; set; } = new List<HmoTariff>();
    public ICollection<PatientHmoEnrollment> Enrollments { get; set; } = new List<PatientHmoEnrollment>();
}

public class HmoContract : Entity
{
    public Guid HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Terms { get; set; }
    public string? DocumentRef { get; set; }
    public HmoContractStatus Status { get; set; } = HmoContractStatus.Active;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class HmoTariff : Entity
{
    public Guid HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public TariffStatus Status { get; set; } = TariffStatus.Draft;
    public string? SourceFileName { get; set; }
    public string? SourceFileHash { get; set; }
    public Guid? ImportedBy { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<HmoTariffItem> Items { get; set; } = new List<HmoTariffItem>();
}

public class HmoTariffItem : Entity
{
    public Guid HmoTariffId { get; set; }
    public HmoTariff? HmoTariff { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TariffCategory Category { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CoveragePercent { get; set; } = 100m;
    public bool RequiresPriorAuthorization { get; set; }
    public bool IsActive { get; set; } = true;
}
