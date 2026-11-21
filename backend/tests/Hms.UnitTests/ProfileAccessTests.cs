using Hms.Application.Security;
using Hms.Domain.Enums;

namespace Hms.UnitTests;

public sealed class ProfileAccessTests
{
    [Theory]
    [InlineData(StaffRole.HrOffice, false, true)]
    [InlineData(StaffRole.Admin, false, true)]
    [InlineData(StaffRole.Doctor, true, true)]
    [InlineData(StaffRole.Nurse, false, false)]
    public void Employment_is_visible_to_self_hr_and_admin_only(StaffRole role, bool isSelf, bool expected)
    {
        Assert.Equal(expected, ProfileAccess.CanViewEmployment(role, isSelf));
    }

    [Theory]
    [InlineData(StaffRole.Nurse, true)]
    [InlineData(StaffRole.Doctor, true)]
    [InlineData(StaffRole.HrOffice, false)]
    [InlineData(StaffRole.Receptionist, false)]
    public void Vitals_follow_existing_vitals_permission(StaffRole role, bool expected)
    {
        Assert.Equal(expected, ProfileAccess.CanViewVitals(role));
    }

    [Theory]
    [InlineData(StaffRole.Finance, false, true)]
    [InlineData(StaffRole.Nurse, true, true)]
    [InlineData(StaffRole.HrOffice, false, false)]
    public void Finance_is_visible_to_finance_roles_or_private_patients(StaffRole role, bool isPrivate, bool expected)
    {
        Assert.Equal(expected, ProfileAccess.CanViewFinance(role, isPrivate));
    }

    [Theory]
    [InlineData(StaffRole.Nurse, true)]
    [InlineData(StaffRole.Doctor, true)]
    [InlineData(StaffRole.HrOffice, false)]
    public void File_upload_uses_the_existing_patient_files_permission(StaffRole role, bool expected)
    {
        Assert.Equal(expected, ProfileAccess.CanUploadPatientFiles(role));
    }
}
