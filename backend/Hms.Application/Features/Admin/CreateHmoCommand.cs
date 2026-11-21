using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record CreateHmoCommand(CreateHmoCompanyRequest Request) : IRequest<HmoCompanyDto>;

public sealed class CreateHmoCommandHandler(IAdminService admin) : IRequestHandler<CreateHmoCommand, HmoCompanyDto>
{
    public Task<HmoCompanyDto> Handle(CreateHmoCommand request, CancellationToken cancellationToken) =>
        admin.CreateHmoAsync(request.Request, cancellationToken);
}

public sealed class CreateHmoCommandValidator : AbstractValidator<CreateHmoCommand>
{
    public CreateHmoCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.Phone).MaximumLength(40);
    }
}
