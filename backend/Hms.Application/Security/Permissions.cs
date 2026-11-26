using Hms.Domain.Enums;

namespace Hms.Application.Security;

/// <summary>Permission strings used by API policies and Angular route guards.</summary>
public static class Permissions
{
    public const string PatientsRead = "patients.read";
    public const string PatientsProfileRead = "patients.profile.read";
    public const string PatientFilesUpload = "patients.files.upload";
    public const string PatientsCreate = "patients.create";
    public const string PatientsUpdate = "patients.update";
    public const string PatientsRenew = "patients.renew";
    public const string PatientHmoManage = "patients.hmo.manage";

    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsCreate = "appointments.create";
    public const string FundingVerify = "funding.verify";

    public const string StockRead = "stock.read";
    public const string StockManage = "stock.manage";
    public const string LabRequestsRead = "lab.requests.read";
    public const string ProceduresRequestsRead = "procedures.requests.read";

    public const string VitalsRead = "vitals.read";
    public const string VitalsWrite = "vitals.write";

    public const string EncountersRead = "encounters.read";
    public const string EncountersWrite = "encounters.write";
    public const string EncountersSign = "encounters.sign";
    public const string PrescriptionsWrite = "prescriptions.write";
    public const string InvestigationsWrite = "investigations.write";
    public const string SurgeriesWrite = "surgeries.write";
    public const string MedicationAdminister = "medications.administer";
    public const string NurseHandover = "nursing.handover";
    public const string DischargeInitiate = "discharge.initiate";
    public const string DischargeComplete = "discharge.complete";

    public const string PharmacyRead = "pharmacy.read";
    public const string PharmacyDispense = "pharmacy.dispense";
    public const string PharmacyCommit = "pharmacy.commit";
    public const string PharmacyTariffConfirm = "pharmacy.tariff.confirm";
    public const string PharmacyStockManage = "pharmacy.stock.manage";

    public const string BillingRead = "billing.read";
    public const string BillingApprove = "billing.approve";
    public const string BillingNegotiate = "billing.negotiate";
    public const string BillingPayment = "billing.payment";
    public const string AccountBookRead = "accountbook.read";
    public const string AccountBookWrite = "accountbook.write";
    public const string HmoReceivableManage = "hmo.receivable.manage";

    public const string HmoRead = "hmo.read";
    public const string HmoPriorAuthManage = "hmo.priorauth.manage";
    public const string HmoCoverageManage = "hmo.coverage.manage";
    public const string HmoApprove = "hmo.approve";
    public const string HmoCompanyManage = "hmo.company.manage";
    public const string HmoTariffManage = "hmo.tariff.manage";

    public const string FitnessRead = "fitness.read";
    public const string FitnessCreate = "fitness.create";
    public const string FitnessPayment = "fitness.payment";
    public const string FitnessExport = "fitness.export";
    public const string FitnessApprove = "fitness.approve";
    public const string FitnessCertificateUpload = "fitness.certificate.upload";

    public const string HrUsersRead = "hr.users.read";
    public const string HrUsersManage = "hr.users.manage";
    public const string HrPayrollManage = "hr.payroll.manage";
    public const string HrExport = "hr.export";

    public const string AdminFeesManage = "admin.fees.manage";
    public const string AdminHmosManage = "admin.hmos.manage";
    public const string AdminTariffsManage = "admin.tariffs.manage";
    public const string AdminSettingsManage = "admin.settings.manage";
    public const string AuditRead = "audit.read";
}
