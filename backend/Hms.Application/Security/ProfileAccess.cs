using Hms.Domain.Enums;

namespace Hms.Application.Security;

/// <summary>
/// Section-level profile visibility derived from the existing permission catalogue.
/// This is not a second authorization system; it only answers which profile sections may be
/// serialized for a role that has already passed the route/API policy.
/// </summary>
public static class ProfileAccess
{
    public static bool CanViewVitals(StaffRole role) => PermissionCatalog.RoleHas(role, Permissions.VitalsRead);
    public static bool CanViewClinical(StaffRole role) => PermissionCatalog.RoleHas(role, Permissions.EncountersRead);
    public static bool CanViewFitness(StaffRole role) => PermissionCatalog.RoleHas(role, Permissions.FitnessRead);
    public static bool CanUploadPatientFiles(StaffRole role) => PermissionCatalog.RoleHas(role, Permissions.PatientFilesUpload);
    public static bool CanViewFinance(StaffRole role, bool isPrivatePatient) => isPrivatePatient || PermissionCatalog.RoleHas(role, Permissions.BillingRead);
    public static bool CanViewEmployment(StaffRole viewerRole, bool isSelf) => isSelf || viewerRole is StaffRole.HrOffice or StaffRole.Admin;
}
