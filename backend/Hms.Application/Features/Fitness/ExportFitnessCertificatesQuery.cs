using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Fitness;

public sealed record ExportFitnessCertificatesQuery(DateOnly? From, DateOnly? To, Guid? HmoCompanyId, string? Search,
    FitnessStatus? Status = null, FitnessPaymentType? PaymentType = null, FitnessPaymentStatus? PaymentStatus = null) : IRequest<byte[]>;

public sealed class ExportFitnessCertificatesQueryHandler(IFitnessService fitness)
    : IRequestHandler<ExportFitnessCertificatesQuery, byte[]>
{
    public Task<byte[]> Handle(ExportFitnessCertificatesQuery request, CancellationToken cancellationToken) =>
        fitness.ExportCsvAsync(request.From, request.To, request.HmoCompanyId, request.Search,
            request.Status, request.PaymentType, request.PaymentStatus, cancellationToken);
}

public sealed class ExportFitnessCertificatesQueryValidator : AbstractValidator<ExportFitnessCertificatesQuery>
{
    public ExportFitnessCertificatesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
