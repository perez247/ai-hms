using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record CreateHmoContractCommand(Guid HmoCompanyId, CreateHmoContractRequest Request) : IRequest<HmoContractDto>;

public sealed class CreateHmoContractCommandHandler(IAdminService admin)
    : IRequestHandler<CreateHmoContractCommand, HmoContractDto>
{
    public Task<HmoContractDto> Handle(CreateHmoContractCommand request, CancellationToken cancellationToken) =>
        admin.CreateContractAsync(request.HmoCompanyId, request.Request, cancellationToken);
}

public sealed class CreateHmoContractCommandValidator : AbstractValidator<CreateHmoContractCommand>
{
    public CreateHmoContractCommandValidator()
    {
        RuleFor(x => x.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.EndDate).GreaterThan(x => x.Request.StartDate);
    }
}
