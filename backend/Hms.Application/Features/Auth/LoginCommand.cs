using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Auth;

public sealed record LoginCommand(LoginRequest Request, string? IpAddress, string? UserAgent) : IRequest<AuthResponse>;

public sealed class LoginCommandHandler(IAuthService auth) : IRequestHandler<LoginCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        auth.LoginAsync(request.Request, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Request.Password).NotEmpty().MaximumLength(200);
    }
}
