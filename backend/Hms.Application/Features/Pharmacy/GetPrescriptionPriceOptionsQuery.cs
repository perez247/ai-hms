using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Pharmacy;

public sealed record GetPrescriptionPriceOptionsQuery(Guid EncounterId) : IRequest<PrescriptionPriceOptionsDto>;

public sealed class GetPrescriptionPriceOptionsQueryHandler(IPharmacyService pharmacy)
    : IRequestHandler<GetPrescriptionPriceOptionsQuery, PrescriptionPriceOptionsDto>
{
    public Task<PrescriptionPriceOptionsDto> Handle(GetPrescriptionPriceOptionsQuery request, CancellationToken cancellationToken) =>
        pharmacy.GetPriceOptionsAsync(request.EncounterId, cancellationToken);
}

public sealed class GetPrescriptionPriceOptionsQueryValidator : AbstractValidator<GetPrescriptionPriceOptionsQuery>
{
    public GetPrescriptionPriceOptionsQueryValidator() => RuleFor(x => x.EncounterId).NotEmpty();
}
