using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Auth;

public sealed record RefreshTokenCommand(RefreshRequest Request, string? IpAddress, string? UserAgent) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandHandler(IAuthService auth) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        auth.RefreshAsync(request.Request.RefreshToken, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Request.RefreshToken).NotEmpty().MaximumLength(512);
    }
}
