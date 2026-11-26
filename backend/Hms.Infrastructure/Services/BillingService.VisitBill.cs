using Hms.Application.Common;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>One bill per patient visit: all services are charged to the bill created at check-in.</summary>
public sealed partial class BillingService
{
    public async Task<Bill> GetOrCreateVisitBillAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var existing = await db.Bills
            .FirstOrDefaultAsync(x => x.Kind == BillKind.Visit && x.AppointmentId == appointment.Id && x.Status != BillStatus.Void, cancellationToken);
        if (existing is not null) return existing;

        var quote = await appointmentPricing.QuoteAsync(appointment.Reason, appointment.FundingSource,
            appointment.HmoEnrollmentId, cancellationToken);
        var bill = await CreateVisitBillAsync(appointment.PatientId, appointment.Id, null, null, appointment.FundingSource,
            appointment.HmoEnrollmentId, quote.UnitPrice, quote.CoveragePercent, appointment.Reason,
            appointment.AppointmentNumber, cancellationToken);
        return bill;
    }

    public async Task<Bill> GetOrCreateVisitBillAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        if (encounter.AppointmentId is not null)
        {
            var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == encounter.AppointmentId, cancellationToken);
            if (appointment is not null)
            {
                var bill = await GetOrCreateVisitBillAsync(appointment, cancellationToken);
                // The check-in bill predates the encounter; link it so HMO PAs, results and the
                // encounter detail page all resolve the same visit bill.
                if (bill.EncounterId is null)
                {
                    bill.EncounterId = encounter.Id;
                    bill.AdmissionId ??= encounter.AdmissionId;
                    await db.SaveChangesAsync(cancellationToken);
                }
                return bill;
            }
        }
        if (encounter.AdmissionId is not null)
        {
            // Every encounter under one admission shares the admission's visit bill, so bed days,
            // ward medication and later reviews all bill to the same account at discharge.
            var admissionBill = await db.Bills.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Kind == BillKind.Visit && x.AdmissionId == encounter.AdmissionId && x.Status != BillStatus.Void, cancellationToken);
            if (admissionBill is not null) return admissionBill;
        }
        var existing = await db.Bills.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Kind == BillKind.Visit && x.EncounterId == encounter.Id && x.Status != BillStatus.Void, cancellationToken);
        if (existing is not null) return existing;
        return await CreateVisitBillAsync(encounter.PatientId, null, encounter.Id, encounter.AdmissionId, encounter.FundingSource,
            encounter.HmoEnrollmentId, 0m, 0m, null, encounter.EncounterNumber, cancellationToken);
    }

    private async Task<Bill> CreateVisitBillAsync(Guid patientId, Guid? appointmentId, Guid? encounterId, Guid? admissionId,
        FundingSource fundingSource, Guid? hmoEnrollmentId, decimal appointmentFee, decimal coveragePercent,
        string? reason, string reference, CancellationToken cancellationToken)
    {
        Guid? hmoCompanyId = null;
        if (fundingSource == FundingSource.Hmo && hmoEnrollmentId is not null)
            hmoCompanyId = await db.PatientHmoEnrollments.AsNoTracking()
                .Where(x => x.Id == hmoEnrollmentId).Select(x => (Guid?)x.HmoCompanyId).FirstOrDefaultAsync(cancellationToken);
        var bill = new Bill
        {
            BillNumber = await numbers.NextAsync("bill_number_seq", "BILL", cancellationToken),
            PatientId = patientId,
            Kind = BillKind.Visit,
            AppointmentId = appointmentId,
            EncounterId = encounterId,
            AdmissionId = admissionId,
            PayerType = fundingSource == FundingSource.Hmo ? PayerType.Hmo : PayerType.SelfPay,
            HmoEnrollmentId = hmoEnrollmentId,
            HmoCompanyId = hmoCompanyId,
            Status = BillStatus.Draft,
            CreatedBy = user.RequireId()
        };
        if (appointmentFee > 0)
        {
            var line = new BillLine
            {
                BillId = bill.Id, Category = BillLineCategory.Appointment,
                Description = string.IsNullOrWhiteSpace(reason) ? $"Appointment {reference}" : $"Appointment — {reason}",
                Quantity = 1m, UnitPrice = appointmentFee,
                SourceType = ChargeSourceType.Appointment, SourceId = appointmentId ?? bill.Id,
                SourceKey = $"visit-appointment:{appointmentId ?? bill.Id}",
                ChargeDate = DateTimeOffset.UtcNow, CreatedBy = user.RequireId()
            };
            BillingCalculator.SetLineAmounts(line, coveragePercent, coveragePercent > 0);
            bill.Lines.Add(line);
            db.BillLines.Add(line);
            BillingCalculator.Recalculate(bill);
        }
        db.Bills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.visit-created", nameof(Bill), bill.Id.ToString(), patientId, null, bill.Id,
            after: new { bill.BillNumber, bill.PayerType }, cancellationToken: cancellationToken);
        return bill;
    }
}
