using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class Medication : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Form { get; set; }
    public string? Strength { get; set; }
    public string Unit { get; set; } = "unit";
    public decimal UnitPrice { get; set; }
    public string? TariffCode { get; set; }
    public decimal StockOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class StockMovement : Entity
{
    public Guid MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Prescription : Entity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid DoctorId { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.SentToPharmacy;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentToPharmacyAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    public ICollection<Dispense> Dispenses { get; set; } = new List<Dispense>();
}

public class PrescriptionItem : Entity
{
    public Guid PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public Guid MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public string Dose { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; } = 1;
    public decimal QuantityPrescribed { get; set; }
    public decimal QuantityDispensed { get; set; }
    public bool IsPrn { get; set; }
    public string? Instructions { get; set; }
}

/// <summary>
/// Pharmacy stock issue. Outpatient items create bill lines immediately; inpatient items
/// are charged later when the Nurse records <see cref="MedicationAdministration"/>.
/// </summary>
public class Dispense : Entity
{
    public Guid PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid PharmacyUserId { get; set; }
    public DispenseStatus Status { get; set; } = DispenseStatus.Dispensed;
    public DateTimeOffset? DispensedAt { get; set; }
    public DateTimeOffset? TariffConfirmedAt { get; set; }
    public Guid? TariffConfirmedBy { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }

    public ICollection<DispenseItem> Items { get; set; } = new List<DispenseItem>();
}

public class DispenseItem : Entity
{
    public Guid DispenseId { get; set; }
    public Dispense? Dispense { get; set; }
    public Guid PrescriptionItemId { get; set; }
    public PrescriptionItem? PrescriptionItem { get; set; }
    public Guid MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? TariffItemId { get; set; }
    public HmoTariffItem? TariffItem { get; set; }
    public decimal HmoAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public ICollection<MedicationAdministration> Administrations { get; set; } = new List<MedicationAdministration>();
}

/// <summary>Nurse evidence of an inpatient medication dose; this is the inpatient medication billing trigger.</summary>
public class MedicationAdministration : Entity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid PrescriptionItemId { get; set; }
    public PrescriptionItem? PrescriptionItem { get; set; }
    public Guid DispenseItemId { get; set; }
    public DispenseItem? DispenseItem { get; set; }
    public Guid NurseId { get; set; }
    public DateTimeOffset AdministeredAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal DoseGiven { get; set; }
    public string Route { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
