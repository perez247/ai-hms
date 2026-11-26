using Hms.Domain.Enums;

namespace Hms.Application.Security;

/// <summary>
/// Maps each staff role to its allowed permissions. Admin deliberately receives the union
/// of every permission; all other roles are restricted here and at controller policies.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<string> All =
    [
        Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.PatientFilesUpload, Permissions.PatientsCreate, Permissions.PatientsUpdate, Permissions.PatientsRenew, Permissions.PatientHmoManage,
        Permissions.AppointmentsRead, Permissions.AppointmentsCreate, Permissions.FundingVerify,
        Permissions.StockRead, Permissions.StockManage, Permissions.LabRequestsRead, Permissions.ProceduresRequestsRead,
        Permissions.VitalsRead, Permissions.VitalsWrite,
        Permissions.EncountersRead, Permissions.EncountersWrite, Permissions.EncountersSign, Permissions.PrescriptionsWrite,
        Permissions.InvestigationsWrite, Permissions.SurgeriesWrite, Permissions.MedicationAdminister,
        Permissions.DischargeInitiate, Permissions.DischargeComplete, Permissions.NurseHandover,
        Permissions.PharmacyRead, Permissions.PharmacyDispense, Permissions.PharmacyCommit,
        Permissions.PharmacyTariffConfirm, Permissions.PharmacyStockManage,
        Permissions.BillingRead, Permissions.BillingApprove, Permissions.BillingNegotiate,
        Permissions.BillingPayment, Permissions.AccountBookRead, Permissions.AccountBookWrite,
        Permissions.HmoReceivableManage, Permissions.HmoRead, Permissions.HmoPriorAuthManage,
        Permissions.HmoCoverageManage, Permissions.HmoApprove, Permissions.HmoCompanyManage, Permissions.HmoTariffManage,
        Permissions.FitnessRead, Permissions.FitnessCreate, Permissions.FitnessApprove, Permissions.FitnessCertificateUpload, Permissions.FitnessPayment, Permissions.FitnessExport,
        Permissions.HrUsersRead, Permissions.HrUsersManage, Permissions.HrPayrollManage, Permissions.HrExport,
        Permissions.AdminFeesManage, Permissions.AdminHmosManage, Permissions.AdminTariffsManage,
        Permissions.AdminSettingsManage, Permissions.AuditRead
    ];

    private static readonly IReadOnlyDictionary<StaffRole, IReadOnlyList<string>> Map =
        new Dictionary<StaffRole, IReadOnlyList<string>>
        {
            [StaffRole.Receptionist] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.PatientsCreate, Permissions.PatientsUpdate, Permissions.PatientsRenew,
                Permissions.PatientHmoManage, Permissions.AppointmentsRead, Permissions.AppointmentsCreate, Permissions.FundingVerify,
                Permissions.FitnessRead, Permissions.FitnessCreate, Permissions.BillingRead, Permissions.PatientFilesUpload
            ],
            [StaffRole.Nurse] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.VitalsRead, Permissions.VitalsWrite,
                Permissions.AppointmentsRead, Permissions.EncountersRead, Permissions.PharmacyRead, Permissions.FundingVerify,
                Permissions.MedicationAdminister, Permissions.NurseHandover,
                Permissions.FitnessRead, Permissions.FitnessCreate, Permissions.FitnessCertificateUpload,
                Permissions.PatientFilesUpload
            ],
            [StaffRole.Doctor] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.AppointmentsRead, Permissions.VitalsRead,
                Permissions.EncountersRead, Permissions.EncountersWrite, Permissions.EncountersSign, Permissions.PrescriptionsWrite,
                Permissions.InvestigationsWrite, Permissions.SurgeriesWrite, Permissions.DischargeInitiate,
                Permissions.DischargeComplete, Permissions.FitnessRead, Permissions.FitnessApprove,
                Permissions.BillingRead, Permissions.PatientFilesUpload, Permissions.FundingVerify,
                Permissions.LabRequestsRead, Permissions.ProceduresRequestsRead
            ],
            [StaffRole.Pharmacy] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.EncountersRead, Permissions.PharmacyRead,
                Permissions.PharmacyDispense, Permissions.PharmacyCommit, Permissions.PharmacyTariffConfirm,
                Permissions.PharmacyStockManage, Permissions.BillingRead, Permissions.StockRead, Permissions.StockManage,
                Permissions.LabRequestsRead, Permissions.InvestigationsWrite, Permissions.ProceduresRequestsRead, Permissions.SurgeriesWrite,
                // Pharmacy works the Bills page alongside Finance: HMO lookup, approve, negotiate and settle.
                // The account book remains Finance-only.
                Permissions.HmoRead, Permissions.BillingApprove, Permissions.BillingNegotiate, Permissions.BillingPayment
            ],
            [StaffRole.Finance] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.BillingRead, Permissions.BillingApprove,
                Permissions.BillingNegotiate, Permissions.BillingPayment, Permissions.AccountBookRead,
                Permissions.AccountBookWrite, Permissions.HmoReceivableManage, Permissions.HmoRead,
                Permissions.FitnessRead,
                Permissions.FitnessPayment, Permissions.FitnessExport, Permissions.FundingVerify
            ],
            [StaffRole.HmoOffice] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.EncountersRead, Permissions.PharmacyRead,
                Permissions.BillingRead, Permissions.HmoRead, Permissions.HmoPriorAuthManage,
                Permissions.HmoCoverageManage, Permissions.HmoApprove, Permissions.HmoCompanyManage,
                Permissions.HmoTariffManage, Permissions.FitnessRead, Permissions.FundingVerify
            ],
            [StaffRole.HrOffice] =
            [
                Permissions.PatientsProfileRead, Permissions.HrUsersRead, Permissions.HrUsersManage,
                Permissions.HrPayrollManage, Permissions.HrExport
            ],
            [StaffRole.Lab] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.AppointmentsRead,
                Permissions.EncountersRead, Permissions.PharmacyRead,
                Permissions.LabRequestsRead, Permissions.InvestigationsWrite
            ],
            [StaffRole.Admin] = All
        };

    public static IReadOnlyList<string> ForRole(StaffRole role) =>
        Map.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool RoleHas(StaffRole role, string permission) => ForRole(role).Contains(permission);
}
