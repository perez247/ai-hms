using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hms.Infrastructure.Jobs;

/// <summary>Daily in-process expiry sweep for registrations, HMO contracts and tariffs.</summary>
public sealed class ExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Expiry worker failed");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warning30 = today.AddDays(30);
        var warning90 = today.AddDays(90);
        var changes = 0;

        var expiredPatients = await db.Patients
            .Where(x => x.Status == PatientStatus.Active && x.CurrentRegistrationExpiresAt < today)
            .ToListAsync(cancellationToken);
        foreach (var patient in expiredPatients)
        {
            patient.Status = PatientStatus.Expired;
            patient.UpdatedAt = DateTimeOffset.UtcNow;
            changes++;
        }

        var expiringPatients = await db.Patients.CountAsync(x => x.Status == PatientStatus.Active
            && x.CurrentRegistrationExpiresAt >= today && x.CurrentRegistrationExpiresAt <= warning30, cancellationToken);
        if (expiringPatients > 0) logger.LogWarning("{Count} patient registrations expire within 30 days", expiringPatients);

        var expiredContracts = await db.HmoContracts
            .Where(x => x.Status == HmoContractStatus.Active && x.EndDate < today)
            .ToListAsync(cancellationToken);
        foreach (var contract in expiredContracts)
        {
            contract.Status = HmoContractStatus.Expired;
            changes++;
        }

        var expiringContracts = await db.HmoContracts
            .Where(x => x.Status == HmoContractStatus.Active && x.EndDate >= today && x.EndDate <= warning90)
            .ToListAsync(cancellationToken);
        foreach (var contract in expiringContracts)
        {
            contract.Status = HmoContractStatus.Expiring;
            changes++;
        }

        var expiredTariffs = await db.HmoTariffs
            .Where(x => x.Status == TariffStatus.Active && x.EffectiveTo != null && x.EffectiveTo < today)
            .ToListAsync(cancellationToken);
        foreach (var tariff in expiredTariffs)
        {
            tariff.Status = TariffStatus.Expired;
            changes++;
        }

        if (changes > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Expiry worker applied {Count} status changes", changes);
        }
    }
}
