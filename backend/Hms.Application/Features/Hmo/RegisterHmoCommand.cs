using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

public sealed record RegisterHmoCommand(RegisterHmoRequest Request) : IRequest<HmoDirectoryEntryDto>;

public sealed class RegisterHmoCommandHandler(IHmoService hmo) : IRequestHandler<RegisterHmoCommand, HmoDirectoryEntryDto>
{
    public Task<HmoDirectoryEntryDto> Handle(RegisterHmoCommand request, CancellationToken cancellationToken) =>
        hmo.RegisterAsync(request.Request, cancellationToken);
}

public sealed class RegisterHmoCommandValidator : AbstractValidator<RegisterHmoCommand>
{
    public RegisterHmoCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
    }
}
