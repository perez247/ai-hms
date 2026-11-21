using System.Globalization;
using System.Text;
using System.Text.Json;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Admin fee and HMO partner/contract configuration.</summary>
public sealed partial class AdminService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit) : IAdminService
{

    public async Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default) =>
        await db.FeeConfigurations.AsNoTracking().OrderBy(x => x.Code).ThenByDescending(x => x.EffectiveFrom)
            .Select(x => new FeeDto(x.Id, x.Code, x.Amount, x.Currency, x.EffectiveFrom, x.EffectiveTo, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<FeeDto> CreateFeeAsync(CreateFeeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0) throw new AppConflictException("Fee amount cannot be negative.");
        if (request.EffectiveTo is not null && request.EffectiveTo <= request.EffectiveFrom)
            throw new AppConflictException("Fee end date must be after the start date.");
        if (await db.FeeConfigurations.AnyAsync(x => x.Code == request.Code && x.EffectiveFrom == request.EffectiveFrom, cancellationToken))
            throw new AppConflictException("A fee for this code and effective date already exists.");
        var fee = new FeeConfiguration
        {
            Code = request.Code,
            Amount = request.Amount,
            Currency = request.Currency,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            CreatedBy = user.RequireId()
        };
        db.FeeConfigurations.Add(fee);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admin.fee-created", nameof(FeeConfiguration), fee.Id.ToString(), after: new { fee.Code, fee.Amount, fee.EffectiveFrom }, cancellationToken: cancellationToken);
        return new FeeDto(fee.Id, fee.Code, fee.Amount, fee.Currency, fee.EffectiveFrom, fee.EffectiveTo, fee.IsActive);
    }

    public async Task<IReadOnlyList<HmoCompanyDto>> GetHmosAsync(CancellationToken cancellationToken = default) =>
        await db.HmoCompanies.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new HmoCompanyDto(x.Id, x.Code, x.Name, x.ContactPerson, x.Email, x.Phone, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<HmoCompanyDto> CreateHmoAsync(CreateHmoCompanyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new AppConflictException("HMO code and name are required.");
        if (await db.HmoCompanies.AnyAsync(x => x.Code == request.Code, cancellationToken))
            throw new AppConflictException("HMO code already exists.");
        var hmo = new HmoCompany
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            ContractTerms = request.ContractTerms,
            MetadataJson = NormalizeJson(request.MetadataJson),
            CreatedBy = user.RequireId()
        };
        db.HmoCompanies.Add(hmo);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admin.hmo-created", nameof(HmoCompany), hmo.Id.ToString(), after: new { hmo.Code, hmo.Name }, cancellationToken: cancellationToken);
        return new HmoCompanyDto(hmo.Id, hmo.Code, hmo.Name, hmo.ContactPerson, hmo.Email, hmo.Phone, hmo.Address, hmo.IsActive);
    }

    public async Task<HmoContractDto> CreateContractAsync(Guid hmoCompanyId, CreateHmoContractRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.HmoCompanies.AnyAsync(x => x.Id == hmoCompanyId, cancellationToken))
            throw new AppNotFoundException("HMO company not found.");
        if (request.EndDate <= request.StartDate) throw new AppConflictException("Contract end date must be after the start date.");
        var existing = await db.HmoContracts.Where(x => x.HmoCompanyId == hmoCompanyId && x.Status == HmoContractStatus.Active).ToListAsync(cancellationToken);
        foreach (var contract in existing)
        {
            contract.Status = contract.EndDate < DateOnly.FromDateTime(DateTime.UtcNow) ? HmoContractStatus.Expired : HmoContractStatus.Terminated;
        }
        if (existing.Count > 0) await db.SaveChangesAsync(cancellationToken);
        var created = new HmoContract
        {
            HmoCompanyId = hmoCompanyId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Terms = request.Terms,
            DocumentRef = request.DocumentRef,
            Status = HmoContractStatus.Active,
            CreatedBy = user.RequireId()
        };
        db.HmoContracts.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admin.hmo-contract-created", nameof(HmoContract), created.Id.ToString(), after: new { created.StartDate, created.EndDate }, cancellationToken: cancellationToken);
        return new HmoContractDto(created.Id, created.StartDate, created.EndDate, created.Status);
    }
}
