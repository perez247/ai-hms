using System.Text;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Hms.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Identity user administration, payroll fields, disablement and CSV export.</summary>
public sealed class HrService(
    HmsDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IAuditWriter audit) : IHrService
{
    public async Task<PagedResult<UserDto>> SearchAsync(bool? active, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.AsNoTracking().AsQueryable();
        if (active is not null) query = query.Where(x => x.IsActive == active);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<UserDto>(rows.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new AppConflictException("Full name is required.");
        if (string.IsNullOrWhiteSpace(request.Email)) throw new AppConflictException("Email is required.");
        if (request.Password.Length < 10) throw new AppConflictException("Password must be at least 10 characters.");
        if (await userManager.FindByEmailAsync(request.Email.Trim()) is not null)
            throw new AppConflictException("A user with this email already exists.");
        if (!string.IsNullOrWhiteSpace(request.EmployeeNumber)
            && await db.Users.AnyAsync(x => x.EmployeeNumber == request.EmployeeNumber, cancellationToken))
            throw new AppConflictException("Employee number is already in use.");

        var user = new AppUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            StaffRole = request.StaffRole,
            EmployeeNumber = request.EmployeeNumber,
            PhoneNumber = request.Phone,
            SalaryAmount = request.SalaryAmount,
            BankName = request.BankName,
            BankAccountName = request.BankAccountName,
            BankAccountNumber = request.BankAccountNumber,
            HiredOn = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new AppConflictException(string.Join("; ", result.Errors.Select(x => x.Description)));
        await audit.WriteAsync("hr.user-created", nameof(AppUser), user.Id.ToString(),
            after: new { user.FullName, user.Email, user.StaffRole, user.EmployeeNumber }, cancellationToken: cancellationToken);
        return Map(user);
    }

    public async Task<UserDto> UpdatePayrollAsync(Guid userId, UpdatePayrollRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppNotFoundException("User not found.");
        if (request.SalaryAmount is < 0) throw new AppConflictException("Salary cannot be negative.");
        var before = new { user.SalaryAmount, user.BankName, user.BankAccountName, user.BankAccountNumber };
        user.SalaryAmount = request.SalaryAmount;
        user.BankName = request.BankName;
        user.BankAccountName = request.BankAccountName;
        user.BankAccountNumber = request.BankAccountNumber;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("hr.payroll-updated", nameof(AppUser), user.Id.ToString(), before: before,
            after: new { user.SalaryAmount, user.BankName, user.BankAccountName, user.BankAccountNumber }, cancellationToken: cancellationToken);
        return Map(user);
    }

    public async Task<UserDto> DisableAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppNotFoundException("User not found.");
        if (user.Id == currentUser.Id) throw new AppConflictException("You cannot disable your own account.");
        if (!user.IsActive) return Map(user);
        user.IsActive = false;
        user.DeactivatedAt = DateTimeOffset.UtcNow;
        user.DeactivationReason = reason;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("hr.user-disabled", nameof(AppUser), user.Id.ToString(), before: new { IsActive = true },
            after: new { user.IsActive, reason }, cancellationToken: cancellationToken);
        return Map(user);
    }

    public async Task<byte[]> ExportCsvAsync(bool? active, CancellationToken cancellationToken = default)
    {
        var result = await SearchAsync(active, 1, 10000, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("EmployeeNumber,FullName,Email,Role,Phone,Active,Salary,Bank,BankAccountName,BankAccountNumber,HiredOn,DeactivatedAt");
        foreach (var user in result.Items)
        {
            sb.AppendLine(string.Join(',', Csv(user.EmployeeNumber), Csv(user.FullName), Csv(user.Email),
                Csv(user.StaffRole.ToString()), Csv(user.Phone), user.IsActive, user.SalaryAmount?.ToString("0.00") ?? "",
                Csv(user.BankName), Csv(user.BankAccountName), Csv(user.BankAccountNumber),
                user.HiredOn?.ToString("yyyy-MM-dd") ?? "", user.DeactivatedAt?.ToString("O") ?? ""));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static UserDto Map(AppUser user) => new(
        user.Id, user.FullName, user.Email ?? string.Empty, user.StaffRole, user.EmployeeNumber, user.PhoneNumber,
        user.IsActive, user.SalaryAmount, user.BankName, user.BankAccountName, user.BankAccountNumber,
        user.HiredOn, user.DeactivatedAt);

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
