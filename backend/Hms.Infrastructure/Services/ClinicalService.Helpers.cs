using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Fee lookup, validation helpers and response mapping for ClinicalService.</summary>
public sealed partial class ClinicalService
{

    private async Task AddConsultationChargeAsync(Guid billId, Encounter encounter, CancellationToken cancellationToken)
    {
        var amount = await GetFeeAmountAsync(FeeCode.InpatientConsultation, cancellationToken);
        await billing.AddChargeAsync(billId, BillLineCategory.Consultation,
            $"Inpatient consultation {encounter.EncounterNumber}", 1m, amount,
            ChargeSourceType.Encounter, encounter.Id, $"consultation:{encounter.Id}",
            encounter.Id, encounter.AdmissionId, "INPATIENT_CONSULTATION", cancellationToken);
    }

    private async Task<decimal> GetFeeAmountAsync(FeeCode code, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.FeeConfigurations.AsNoTracking()
            .Where(x => x.Code == code && x.IsActive && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => x.Amount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Encounter> RequireOpenEncounterAsync(Guid encounterId, CancellationToken cancellationToken)
    {
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        if (encounter.Status != EncounterStatus.Open) throw new AppConflictException("Encounter is not open.");
        return encounter;
    }

    private async Task EnsureDoctorAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(x => x.Id == doctorId && x.StaffRole == StaffRole.Doctor && x.IsActive, cancellationToken))
            throw new AppConflictException("Selected user is not an active doctor.");
    }

    private async Task EnsureEnrollmentAsync(Guid patientId, Guid? enrollmentId, CancellationToken cancellationToken)
    {
        if (enrollmentId is null) return;
        if (!await db.PatientHmoEnrollments.AnyAsync(x => x.Id == enrollmentId && x.PatientId == patientId && x.IsActive, cancellationToken))
            throw new AppConflictException("HMO enrollment does not belong to this patient or is inactive.");
    }

    private async Task AddParticipantsAsync(IReadOnlyList<Guid> userIds, InvestigationOrder order, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds.Distinct())
        {
            var clinical = await db.Users.AnyAsync(x => x.Id == userId && x.IsActive
                && (x.StaffRole == StaffRole.Doctor || x.StaffRole == StaffRole.Nurse), cancellationToken);
            if (!clinical) throw new AppConflictException("Investigation participants must be active doctors or nurses.");
            if (!order.Participants.Any(x => x.UserId == userId))
                order.Participants.Add(new InvestigationParticipant { InvestigationOrderId = order.Id, UserId = userId });
        }
    }

    private async Task AddParticipantsAsync(IReadOnlyList<Guid> userIds, SurgeryCase surgery, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds.Distinct())
        {
            var clinical = await db.Users.AnyAsync(x => x.Id == userId && x.IsActive
                && (x.StaffRole == StaffRole.Doctor || x.StaffRole == StaffRole.Nurse), cancellationToken);
            if (!clinical) throw new AppConflictException("Surgery participants must be active doctors or nurses.");
            if (!surgery.Participants.Any(x => x.UserId == userId))
                surgery.Participants.Add(new SurgeryParticipant { SurgeryCaseId = surgery.Id, UserId = userId });
        }
    }

    private static VitalDto MapVital(Vital x) => new(
        x.Id, x.RecordedAt, x.TemperatureC, x.Systolic, x.Diastolic, x.Pulse, x.RespiratoryRate,
        x.Spo2, x.WeightKg, x.HeightCm, x.PainScore, x.Notes);
}
