using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Fitness;

public sealed record SearchFitnessCertificatesQuery(DateOnly? From, DateOnly? To, Guid? HmoCompanyId, string? Search, int Page, int PageSize,
    FitnessStatus? Status = null, FitnessPaymentType? PaymentType = null, FitnessPaymentStatus? PaymentStatus = null)
    : IRequest<PagedResult<FitnessCertificateDto>>;

public sealed class SearchFitnessCertificatesQueryHandler(IFitnessService fitness)
    : IRequestHandler<SearchFitnessCertificatesQuery, PagedResult<FitnessCertificateDto>>
{
    public Task<PagedResult<FitnessCertificateDto>> Handle(SearchFitnessCertificatesQuery request, CancellationToken cancellationToken) =>
        fitness.SearchAsync(request.From, request.To, request.HmoCompanyId, request.Search, request.Page, request.PageSize,
            request.Status, request.PaymentType, request.PaymentStatus, cancellationToken);
}

public sealed class SearchFitnessCertificatesQueryValidator : AbstractValidator<SearchFitnessCertificatesQuery>
{
    public SearchFitnessCertificatesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
