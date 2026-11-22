using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record CreateShiftHandoverCommand(CreateShiftHandoverRequest Request) : IRequest<ShiftHandoverDto>;

public sealed class CreateShiftHandoverCommandHandler(INurseService nurse)
    : IRequestHandler<CreateShiftHandoverCommand, ShiftHandoverDto>
{
    public Task<ShiftHandoverDto> Handle(CreateShiftHandoverCommand request, CancellationToken cancellationToken) =>
        nurse.CreateHandoverAsync(request.Request, cancellationToken);
}

public sealed class CreateShiftHandoverCommandValidator : AbstractValidator<CreateShiftHandoverCommand>
{
    public CreateShiftHandoverCommandValidator()
    {
        RuleFor(x => x.Request.ContentHtml).NotEmpty().MaximumLength(50000);
    }
}
