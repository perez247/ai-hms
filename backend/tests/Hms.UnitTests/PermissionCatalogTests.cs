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
    public void Pharmacy_can_work_the_bills_page_but_not_the_account_book()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Pharmacy);
        Assert.Contains(Permissions.BillingApprove, permissions);
        Assert.Contains(Permissions.BillingNegotiate, permissions);
        Assert.Contains(Permissions.BillingPayment, permissions);
        Assert.Contains(Permissions.HmoRead, permissions);
        Assert.Contains(Permissions.PharmacyCommit, permissions);
        Assert.DoesNotContain(Permissions.AccountBookRead, permissions);
        Assert.DoesNotContain(Permissions.AccountBookWrite, permissions);
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
    public void Pharmacy_can_read_bills_but_not_the_account_book()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Pharmacy);
        Assert.Contains(Permissions.BillingRead, permissions);
        Assert.DoesNotContain(Permissions.AccountBookRead, permissions);
        Assert.DoesNotContain(Permissions.AccountBookWrite, permissions);
    }

    [Fact]
    public void Doctor_can_read_lab_requests_and_enter_results_without_finance_access()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Doctor);
        Assert.Contains(Permissions.LabRequestsRead, permissions);
        Assert.Contains(Permissions.InvestigationsWrite, permissions);
        Assert.DoesNotContain(Permissions.AccountBookRead, permissions);
        Assert.DoesNotContain(Permissions.BillingApprove, permissions);
    }

    [Fact]
    public void Finance_cannot_dispense_or_issue_hmo_authorization()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Finance);
        Assert.Contains(Permissions.BillingApprove, permissions);
        Assert.Contains(Permissions.HmoRead, permissions);
        Assert.DoesNotContain(Permissions.PharmacyDispense, permissions);
        Assert.DoesNotContain(Permissions.HmoPriorAuthManage, permissions);
    }

    [Fact]
    public void Receptionist_can_update_basic_patient_demographics_but_not_encounters()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Receptionist);
        Assert.Contains(Permissions.PatientsUpdate, permissions);
        // Clinical encounter content stays doctor-only, even for receptionists.
        Assert.DoesNotContain(Permissions.EncountersWrite, permissions);
    }

    [Fact]
    public void Lab_role_can_enter_test_results_but_cannot_dispense_or_approve_bills()
    {
        var permissions = PermissionCatalog.ForRole(StaffRole.Lab);
        Assert.Contains(Permissions.LabRequestsRead, permissions);
        Assert.Contains(Permissions.InvestigationsWrite, permissions);
        Assert.DoesNotContain(Permissions.PharmacyDispense, permissions);
        Assert.DoesNotContain(Permissions.BillingApprove, permissions);
        Assert.DoesNotContain(Permissions.EncountersWrite, permissions);
    }

    [Theory]
    [InlineData(StaffRole.Receptionist, Permissions.PatientsCreate)]
    [InlineData(StaffRole.Receptionist, Permissions.PatientsUpdate)]
    [InlineData(StaffRole.Nurse, Permissions.VitalsWrite)]
    [InlineData(StaffRole.Doctor, Permissions.DischargeComplete)]
    [InlineData(StaffRole.Pharmacy, Permissions.PharmacyDispense)]
    [InlineData(StaffRole.Pharmacy, Permissions.BillingApprove)]
    [InlineData(StaffRole.Pharmacy, Permissions.BillingPayment)]
    [InlineData(StaffRole.Pharmacy, Permissions.HmoRead)]
    [InlineData(StaffRole.Finance, Permissions.AccountBookWrite)]
    [InlineData(StaffRole.HmoOffice, Permissions.HmoCoverageManage)]
    [InlineData(StaffRole.HrOffice, Permissions.HrUsersManage)]
    [InlineData(StaffRole.Admin, Permissions.AdminTariffsManage)]
    [InlineData(StaffRole.Receptionist, Permissions.FundingVerify)]
    [InlineData(StaffRole.Doctor, Permissions.EncountersSign)]
    [InlineData(StaffRole.Lab, Permissions.LabRequestsRead)]
    [InlineData(StaffRole.Pharmacy, Permissions.InvestigationsWrite)]
    public void Expected_role_permission_is_present(StaffRole role, string permission)
    {
        Assert.Contains(permission, PermissionCatalog.ForRole(role));
    }
}
