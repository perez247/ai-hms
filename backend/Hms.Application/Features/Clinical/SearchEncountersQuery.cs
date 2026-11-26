using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record SearchEncountersQuery(EncounterType? Type, EncounterStatus? Status, DateOnly? Date, string? Search)
    : IRequest<IReadOnlyList<EncounterListItemDto>>;

public sealed class SearchEncountersQueryHandler(IClinicalService clinical)
    : IRequestHandler<SearchEncountersQuery, IReadOnlyList<EncounterListItemDto>>
{
    public Task<IReadOnlyList<EncounterListItemDto>> Handle(SearchEncountersQuery request, CancellationToken cancellationToken) =>
        clinical.SearchEncountersAsync(request.Type, request.Status, request.Date, request.Search, cancellationToken);
}

public sealed class SearchEncountersQueryValidator : AbstractValidator<SearchEncountersQuery>
{
    public SearchEncountersQueryValidator() => RuleFor(x => x.Search).MaximumLength(200);
}
