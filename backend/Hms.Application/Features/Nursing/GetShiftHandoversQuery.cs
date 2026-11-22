using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record GetShiftHandoversQuery(int Page, int PageSize) : IRequest<IReadOnlyList<ShiftHandoverDto>>;

public sealed class GetShiftHandoversQueryHandler(INurseService nurse)
    : IRequestHandler<GetShiftHandoversQuery, IReadOnlyList<ShiftHandoverDto>>
{
    public Task<IReadOnlyList<ShiftHandoverDto>> Handle(GetShiftHandoversQuery request, CancellationToken cancellationToken) =>
        nurse.GetHandoversAsync(request.Page, request.PageSize, cancellationToken);
}

public sealed class GetShiftHandoversQueryValidator : AbstractValidator<GetShiftHandoversQuery>
{
    public GetShiftHandoversQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
