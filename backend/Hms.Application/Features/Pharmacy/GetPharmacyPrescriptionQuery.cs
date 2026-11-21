using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Pharmacy;

public sealed record GetPharmacyPrescriptionQuery(Guid EncounterId) : IRequest<PharmacyPrescriptionDto>;

public sealed class GetPharmacyPrescriptionQueryHandler(IPharmacyService pharmacy)
    : IRequestHandler<GetPharmacyPrescriptionQuery, PharmacyPrescriptionDto>
{
    public Task<PharmacyPrescriptionDto> Handle(GetPharmacyPrescriptionQuery request, CancellationToken cancellationToken) =>
        pharmacy.GetPrescriptionAsync(request.EncounterId, cancellationToken);
}

public sealed class GetPharmacyPrescriptionQueryValidator : AbstractValidator<GetPharmacyPrescriptionQuery>
{
    public GetPharmacyPrescriptionQueryValidator() => RuleFor(x => x.EncounterId).NotEmpty();
}
