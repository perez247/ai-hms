using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record GetNurseVitalsQuery(string PatientNumber, int Page, int PageSize) : IRequest<PatientVitalsPageDto>;

public sealed class GetNurseVitalsQueryHandler(INurseService nurse) : IRequestHandler<GetNurseVitalsQuery, PatientVitalsPageDto>
{
    public Task<PatientVitalsPageDto> Handle(GetNurseVitalsQuery request, CancellationToken cancellationToken) =>
        nurse.GetVitalsAsync(request.PatientNumber, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetNurseVitalsQueryValidator : AbstractValidator<GetNurseVitalsQuery>
{
    public GetNurseVitalsQueryValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
