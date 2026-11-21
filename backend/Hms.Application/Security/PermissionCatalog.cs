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
        Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.PatientFilesUpload, Permissions.PatientsCreate, Permissions.PatientsRenew, Permissions.PatientHmoManage,
        Permissions.AppointmentsRead, Permissions.AppointmentsCreate,
        Permissions.VitalsRead, Permissions.VitalsWrite,
        Permissions.EncountersRead, Permissions.EncountersWrite, Permissions.PrescriptionsWrite,
        Permissions.InvestigationsWrite, Permissions.SurgeriesWrite, Permissions.MedicationAdminister,
        Permissions.DischargeInitiate, Permissions.DischargeComplete,
        Permissions.PharmacyRead, Permissions.PharmacyDispense, Permissions.PharmacyCommit,
        Permissions.PharmacyTariffConfirm, Permissions.PharmacyStockManage,
        Permissions.BillingRead, Permissions.BillingApprove, Permissions.BillingNegotiate,
        Permissions.BillingPayment, Permissions.AccountBookRead, Permissions.AccountBookWrite,
        Permissions.HmoReceivableManage, Permissions.HmoRead, Permissions.HmoPriorAuthManage,
        Permissions.HmoCoverageManage, Permissions.HmoApprove,
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
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.PatientsCreate, Permissions.PatientsRenew,
                Permissions.PatientHmoManage, Permissions.AppointmentsRead, Permissions.AppointmentsCreate,
                Permissions.FitnessRead, Permissions.FitnessCreate, Permissions.BillingRead, Permissions.PatientFilesUpload
            ],
            [StaffRole.Nurse] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.VitalsRead, Permissions.VitalsWrite,
                Permissions.EncountersRead, Permissions.PharmacyRead, Permissions.MedicationAdminister,
                Permissions.FitnessRead, Permissions.FitnessCreate, Permissions.FitnessCertificateUpload,
                Permissions.PatientFilesUpload
            ],
            [StaffRole.Doctor] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.AppointmentsRead, Permissions.VitalsRead,
                Permissions.EncountersRead, Permissions.EncountersWrite, Permissions.PrescriptionsWrite,
                Permissions.InvestigationsWrite, Permissions.SurgeriesWrite, Permissions.DischargeInitiate,
                Permissions.DischargeComplete, Permissions.FitnessRead, Permissions.FitnessApprove,
                Permissions.BillingRead, Permissions.PatientFilesUpload
            ],
            [StaffRole.Pharmacy] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.EncountersRead, Permissions.PharmacyRead,
                Permissions.PharmacyDispense, Permissions.PharmacyCommit, Permissions.PharmacyTariffConfirm,
                Permissions.PharmacyStockManage, Permissions.BillingRead
            ],
            [StaffRole.Finance] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.BillingRead, Permissions.BillingApprove,
                Permissions.BillingNegotiate, Permissions.BillingPayment, Permissions.AccountBookRead,
                Permissions.AccountBookWrite, Permissions.HmoReceivableManage, Permissions.FitnessRead,
                Permissions.FitnessPayment, Permissions.FitnessExport
            ],
            [StaffRole.HmoOffice] =
            [
                Permissions.PatientsRead, Permissions.PatientsProfileRead, Permissions.EncountersRead, Permissions.PharmacyRead,
                Permissions.BillingRead, Permissions.HmoRead, Permissions.HmoPriorAuthManage,
                Permissions.HmoCoverageManage, Permissions.HmoApprove, Permissions.FitnessRead
            ],
            [StaffRole.HrOffice] =
            [
                Permissions.PatientsProfileRead, Permissions.HrUsersRead, Permissions.HrUsersManage,
                Permissions.HrPayrollManage, Permissions.HrExport
            ],
            [StaffRole.Admin] = All
        };

    public static IReadOnlyList<string> ForRole(StaffRole role) =>
        Map.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool RoleHas(StaffRole role, string permission) => ForRole(role).Contains(permission);
}
