using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Staff;

/// <summary>Null staff id means the current user's own profile.</summary>
public sealed record GetStaffProfileQuery(Guid? StaffId) : IRequest<StaffProfileDto>;

public sealed class GetStaffProfileQueryHandler(IStaffProfileService staff)
    : IRequestHandler<GetStaffProfileQuery, StaffProfileDto>
{
    public Task<StaffProfileDto> Handle(GetStaffProfileQuery request, CancellationToken cancellationToken) =>
        staff.GetAsync(request.StaffId, cancellationToken);
}

public sealed class GetStaffProfileQueryValidator : AbstractValidator<GetStaffProfileQuery>
{
    public GetStaffProfileQueryValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty().When(x => x.StaffId is not null).WithMessage("Staff id must not be empty.");
    }
}
