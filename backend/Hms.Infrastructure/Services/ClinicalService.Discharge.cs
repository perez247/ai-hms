using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Inpatient discharge billing and the Finance-clearance gate.</summary>
public sealed partial class ClinicalService
{

    /// <summary>Generates bed-days, commits the final bill and blocks discharge until Finance approves.</summary>
    public async Task<DischargeRequestResult> RequestDischargeAsync(RequestDischargeRequest request, CancellationToken cancellationToken = default)
    {
        var admission = await db.Admissions.Include(x => x.Patient).Include(x => x.Encounters).Include(x => x.BedDays)
            .SingleOrDefaultAsync(x => x.Id == request.AdmissionId, cancellationToken)
            ?? throw new AppNotFoundException("Admission not found.");
        if (admission.Status != AdmissionStatus.Active) throw new AppConflictException("Only an active admission can be discharged.");
        if (string.IsNullOrWhiteSpace(request.DischargeSummary)) throw new AppConflictException("A discharge summary is required.");

        var bill = await db.Bills.AsNoTracking().FirstOrDefaultAsync(x => x.Kind == BillKind.Visit && x.Status != BillStatus.Void && x.AdmissionId == admission.Id)
            ?? throw new AppNotFoundException("Visit bill not found for this admission.");
        var admittedDate = DateOnly.FromDateTime(admission.AdmittedAt.UtcDateTime);
        var dischargeDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var nights = BillingCalculator.BedNights(admittedDate, dischargeDate);
        var feeCode = admission.WardType == WardType.Private ? FeeCode.BedDayPrivate : FeeCode.BedDayGeneral;
        var fallbackRate = await GetFeeAmountAsync(feeCode, cancellationToken);
        var tariffCode = admission.WardType == WardType.Private ? "BED_DAY_PRIVATE" : "BED_DAY_GENERAL";

        for (var i = 0; i < nights; i++)
        {
            var serviceDate = admittedDate.AddDays(i);
            if (admission.BedDays.Any(x => x.ServiceDate == serviceDate)) continue;
            var bedDay = new BedDay
            {
                AdmissionId = admission.Id,
                ServiceDate = serviceDate,
                WardType = admission.WardType,
                Rate = fallbackRate,
                CreatedBy = user.RequireId()
            };
            db.BedDays.Add(bedDay);
            await db.SaveChangesAsync(cancellationToken);
            var line = await billing.AddChargeAsync(bill.Id, BillLineCategory.BedDay,
                $"Bed day ({admission.WardType}) {serviceDate:yyyy-MM-dd}", 1m, fallbackRate,
                ChargeSourceType.BedDay, bedDay.Id, $"bed-day:{bedDay.Id}", null, admission.Id, tariffCode, cancellationToken);
            bedDay.Rate = line.NetAmount;
        }

        admission.Status = AdmissionStatus.PendingDischarge;
        admission.DischargeSummary = request.DischargeSummary;
        admission.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        bill = await billing.CommitAsync(bill.Id, cancellationToken);
        await audit.WriteAsync("admission.discharge-requested", nameof(Admission), admission.Id.ToString(), admission.PatientId, null, bill.Id,
            after: new { admission.DischargeSummary, bill.PatientPayableTotal, bill.HmoPayableTotal }, cancellationToken: cancellationToken);
        return new DischargeRequestResult(admission.Id, bill.Id, bill.PatientPayableTotal, bill.HmoPayableTotal, bill.Status.ToString());
    }

    public async Task<AdmissionDto> CompleteDischargeAsync(Guid admissionId, CancellationToken cancellationToken = default)
    {
        var admission = await db.Admissions.SingleOrDefaultAsync(x => x.Id == admissionId, cancellationToken)
            ?? throw new AppNotFoundException("Admission not found.");
        if (admission.Status == AdmissionStatus.Discharged) return await GetAdmissionAsync(admissionId, cancellationToken);
        if (admission.Status != AdmissionStatus.PendingDischarge)
            throw new AppConflictException("Request discharge before completing it.");
        var bill = await db.Bills.Include(x => x.PriorAuthorization)
            .SingleOrDefaultAsync(x => x.Kind == BillKind.Visit && x.AdmissionId == admissionId && x.Status != BillStatus.Void, cancellationToken)
            ?? throw new AppConflictException("Discharge bill not found.");
        if (bill.Status is not (BillStatus.Approved or BillStatus.Paid))
            throw new AppConflictException("Finance approval is required before the patient can be discharged.");
        BillStateMachine.EnsurePayable(bill);

        admission.Status = AdmissionStatus.Discharged;
        admission.DischargedAt = DateTimeOffset.UtcNow;
        admission.DischargedBy = user.RequireId();
        admission.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admission.discharged", nameof(Admission), admission.Id.ToString(), admission.PatientId, null, bill.Id, cancellationToken: cancellationToken);
        return await GetAdmissionAsync(admissionId, cancellationToken);
    }
}
