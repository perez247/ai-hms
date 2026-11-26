namespace Hms.Domain.Enums;

/// <summary>The nine clinic roles. Admin is the only role with the full permission union.</summary>
public enum StaffRole
{
    Receptionist,
    Nurse,
    Doctor,
    Pharmacy,
    Finance,
    HmoOffice,
    Admin,
    HrOffice,
    Lab
}

public enum Sex { Female, Male, Other, Unknown }
public enum PatientStatus { Active, Expired, Inactive }
public enum AppointmentStatus { Scheduled, CheckedIn, Completed, Cancelled, NoShow }
public enum FundingSource { Private, Hmo }
/// <summary>
/// Payer categories. Private is the default category every patient belongs to; it is represented by
/// a fixed sentinel id rather than an HMO company row so it never appears in HMO/contract/PA workflows.
/// </summary>
public static class Payers
{
    public const string PrivateName = "Private";
    public static readonly Guid PrivateId = new("11111111-1111-1111-1111-111111111111");

    public static bool IsPrivate(Guid payerId) => payerId == PrivateId;
}

/// <summary>The two bookable appointment reasons. Their prices come from the stock/price catalogue.</summary>
public static class AppointmentReasons
{
    public const string Consultation = "Consultation";
    public const string Emergency = "Emergency";

    public static readonly IReadOnlyList<string> All = [Consultation, Emergency];

    public static bool IsValid(string? reason) => reason is Consultation or Emergency;
}

public enum StockItemType { Medication, Test, Procedure, Service }
public enum ServiceRequestStatus { Pending, InProgress, Completed, Cancelled }
public enum ServiceResultStatus { Pending, Entered, Submitted, Amended }
public enum EncounterType { Outpatient, Inpatient }
public enum EncounterStatus { Open, Completed, Cancelled }
public enum EncounterDisposition { Discharge, FollowUp, Admit, ReferToSpecialist }
public enum AdmissionStatus { Active, PendingDischarge, Discharged, Cancelled }
public enum WardType { General, Private }

public enum BillKind { Registration, Appointment, Visit, Emergency, OutpatientEncounter, InpatientAdmission, FitnessCertificate, Manual }
/// <summary>
/// Billing lifecycle. Approved is the payable state; HMO bills must pass HmoPending/HmoApproved first.
/// </summary>
public enum BillStatus { Draft, Committed, HmoPending, HmoApproved, Approved, Paid, Void }
public enum PayerType { SelfPay, Hmo }
/// <summary>Who carries a bill moved to the debt ledger: the patient for self-pay, the HMO otherwise.</summary>
public enum DebtOwner { Patient, Hmo }
public enum BillLineCategory { Registration, RegistrationRenewal, Appointment, Emergency, Consultation, Medication, Investigation, Surgery, BedDay, FitnessCertificate }
public enum ChargeSourceType { RegistrationPeriod, Appointment, Encounter, MedicationDispenseItem, MedicationAdministration, InvestigationOrder, SurgeryCase, BedDay, FitnessCertificate }

public enum PrescriptionStatus { Draft, SentToPharmacy, PartiallyDispensed, Dispensed, Cancelled }
public enum DispenseStatus { Draft, Committed, Dispensed, Cancelled }
public enum InvestigationStatus { Ordered, InProgress, Completed, Cancelled }
public enum SurgeryStatus { Scheduled, InProgress, Completed, Cancelled }
public enum HmoContractStatus { Active, Expiring, Expired, Terminated }
public enum TariffStatus { Draft, Active, Superseded, Expired }
public enum TariffCategory { Consultation, Medication, Investigation, Surgery, BedDay, Other }
public enum PriorAuthorizationStatus { Requested, Issued, Rejected, Expired }
public enum PaymentMethod { Cash, Card, BankTransfer, HmoRemittance, Other }
public enum HmoReceivableStatus { Open, PartiallySettled, Settled, WrittenOff }
public enum AdjustmentStatus { Proposed, Approved, Rejected }
public enum AccountEntryType { Income, Expense }
public enum FitnessStatus { AwaitingNurse, AwaitingDoctor, Approved, Rejected }
public enum FitnessPaymentType { Private, Hmo }
public enum FitnessPaymentStatus { Unpaid, Paid }
public enum StockMovementType { Receipt, Dispense, Administration, Return, Adjustment }
/// <summary>Effective-dated flat fee keys configured by Admin.</summary>
public enum FeeCode
{
    RegistrationNew,
    RegistrationRenewal,
    Appointment,
    Emergency,
    InpatientConsultation,
    BedDayGeneral,
    BedDayPrivate,
    FitnessCertificate
}
