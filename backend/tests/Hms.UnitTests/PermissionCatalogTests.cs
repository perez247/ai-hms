using Hms.Application.Security;
using Hms.Domain.Enums;

namespace Hms.UnitTests;

/// <summary>Role-permission separation, especially Pharmacy/Finance/HMO boundaries.</summary>
public sealed class PermissionCatalogTests
{
    [Fact]
    public void Admin_has_every_permission()
    {
        var admin = PermissionCatalog.ForRole(StaffRole.Admin).ToHashSet();
        Assert.Equal(PermissionCatalog.All.ToHashSet(), admin);
    }

    [Fact]
    public void Pharmacy_cannot_approve_bills_or_payments()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Pharmacy);
        Assert.DoesNotContain(Permissions.BillingApprove, permissions);
        Assert.DoesNotContain(Permissions.BillingPayment, permissions);
        Assert.Contains(Permissions.PharmacyCommit, permissions);
    }

    [Fact]
    public void Hmo_office_cannot_approve_finance_or_record_payment()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.HmoOffice);
        Assert.Contains(Permissions.HmoApprove, permissions);
        Assert.DoesNotContain(Permissions.BillingApprove, permissions);
        Assert.DoesNotContain(Permissions.BillingPayment, permissions);
    }

    [Fact]
    public void Finance_cannot_dispense_or_issue_hmo_authorization()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Finance);
        Assert.Contains(Permissions.BillingApprove, permissions);
        Assert.DoesNotContain(Permissions.PharmacyDispense, permissions);
        Assert.DoesNotContain(Permissions.HmoPriorAuthManage, permissions);
    }

    [Theory]
    [InlineData(StaffRole.Receptionist, Permissions.PatientsCreate)]
    [InlineData(StaffRole.Nurse, Permissions.VitalsWrite)]
    [InlineData(StaffRole.Doctor, Permissions.DischargeComplete)]
    [InlineData(StaffRole.Pharmacy, Permissions.PharmacyDispense)]
    [InlineData(StaffRole.Finance, Permissions.AccountBookWrite)]
    [InlineData(StaffRole.HmoOffice, Permissions.HmoCoverageManage)]
    [InlineData(StaffRole.HrOffice, Permissions.HrUsersManage)]
    [InlineData(StaffRole.Admin, Permissions.AdminTariffsManage)]
    public void Expected_role_permission_is_present(StaffRole role, string permission)
    {
        Assert.Contains(permission, PermissionCatalog.ForRole(role));
    }
}
