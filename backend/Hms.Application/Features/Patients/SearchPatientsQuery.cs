using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record SearchPatientsQuery(string? Search, int Page, int PageSize) : IRequest<PagedResult<PatientSearchItem>>;

public sealed class SearchPatientsQueryHandler(IPatientService patients) : IRequestHandler<SearchPatientsQuery, PagedResult<PatientSearchItem>>
{
    public Task<PagedResult<PatientSearchItem>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken) =>
        patients.SearchAsync(request.Search, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
