using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class StockMovement : Entity
{
    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public Guid MedicationId { get => StockItemId; set => StockItemId = value; }
    public StockItem? Medication { get => StockItem; set => StockItem = value; }
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
    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public Guid MedicationId { get => StockItemId; set => StockItemId = value; }
    public StockItem? Medication { get => StockItem; set => StockItem = value; }
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
    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public Guid MedicationId { get => StockItemId; set => StockItemId = value; }
    public StockItem? Medication { get => StockItem; set => StockItem = value; }
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
    /// <summary>Captured nurse name at administration time for display on the bill page.</summary>
    public string NurseName { get; set; } = string.Empty;
    public DateTimeOffset AdministeredAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal DoseGiven { get; set; }
    public string Route { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? Notes { get; set; }
    /// <summary>Whether the patient accepted the medication.</summary>
    public bool Accepted { get; set; }
    /// <summary>Required when Accepted is false; explains why the medication was not given.</summary>
    public string? NonAcceptanceReason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
