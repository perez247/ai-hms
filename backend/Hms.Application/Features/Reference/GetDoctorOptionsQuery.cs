using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Reference;

public sealed record GetDoctorOptionsQuery : IRequest<IReadOnlyList<DoctorOptionDto>>;

public sealed class GetDoctorOptionsQueryHandler(IReferenceService reference) : IRequestHandler<GetDoctorOptionsQuery, IReadOnlyList<DoctorOptionDto>>
{
    public Task<IReadOnlyList<DoctorOptionDto>> Handle(GetDoctorOptionsQuery request, CancellationToken cancellationToken) =>
        reference.GetDoctorsAsync(cancellationToken);
}
