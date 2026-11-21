using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// Own-profile reads are available to every authenticated staff member; HR/Admin may read any
/// staff member. Employment fields are omitted for anyone who is not the subject or HR/Admin.
/// </summary>
public sealed class StaffProfileService(HmsDbContext db, ICurrentUser currentUser) : IStaffProfileService
{
    public async Task<StaffProfileDto> GetAsync(Guid? staffId, CancellationToken cancellationToken = default)
    {
        var currentId = currentUser.RequireId();
        var role = currentUser.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        var targetId = staffId ?? currentId;
        var isSelf = targetId == currentId;
        if (!isSelf && !ProfileAccess.CanViewEmployment(role, false))
            throw new AppForbiddenException("Only HR or Admin can view another staff profile.");

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == targetId, cancellationToken)
            ?? throw new AppNotFoundException("Staff member not found.");
        var employment = ProfileAccess.CanViewEmployment(role, isSelf);
        return new StaffProfileDto(
            user.Id, user.FullName, user.Email ?? string.Empty, user.PhoneNumber, user.StaffRole, user.IsActive,
            employment, employment ? user.HiredOn : null, employment ? user.EmployeeNumber : null,
            employment ? user.SalaryAmount : null, employment ? user.BankName : null,
            employment ? user.BankAccountName : null, employment ? user.BankAccountNumber : null);
    }
}
