using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record GetClinicSettingsQuery : IRequest<IReadOnlyList<ClinicSettingDto>>;

public sealed class GetClinicSettingsQueryHandler(IAdminService admin)
    : IRequestHandler<GetClinicSettingsQuery, IReadOnlyList<ClinicSettingDto>>
{
    public Task<IReadOnlyList<ClinicSettingDto>> Handle(GetClinicSettingsQuery request, CancellationToken cancellationToken) =>
        admin.GetSettingsAsync(cancellationToken);
}
