using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hr;

public sealed record DisableUserCommand(Guid UserId, DisableUserRequest Request) : IRequest<UserDto>;

public sealed class DisableUserCommandHandler(IHrService hr) : IRequestHandler<DisableUserCommand, UserDto>
{
    public Task<UserDto> Handle(DisableUserCommand request, CancellationToken cancellationToken) =>
        hr.DisableAsync(request.UserId, request.Request.Reason, cancellationToken);
}

public sealed class DisableUserCommandValidator : AbstractValidator<DisableUserCommand>
{
    public DisableUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(1000);
    }
}
