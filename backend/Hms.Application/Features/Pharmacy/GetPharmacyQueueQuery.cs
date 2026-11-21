using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Pharmacy;

public sealed record GetPharmacyQueueQuery : IRequest<IReadOnlyList<PharmacyQueueItemDto>>;

public sealed class GetPharmacyQueueQueryHandler(IPharmacyService pharmacy)
    : IRequestHandler<GetPharmacyQueueQuery, IReadOnlyList<PharmacyQueueItemDto>>
{
    public Task<IReadOnlyList<PharmacyQueueItemDto>> Handle(GetPharmacyQueueQuery request, CancellationToken cancellationToken) =>
        pharmacy.GetQueueAsync(cancellationToken);
}
