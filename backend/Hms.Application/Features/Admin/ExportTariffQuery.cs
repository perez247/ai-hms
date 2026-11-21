using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record ExportTariffQuery(Guid HmoCompanyId, Guid TariffId, string? Category, string? Code) : IRequest<byte[]>;

public sealed class ExportTariffQueryHandler(IAdminService admin) : IRequestHandler<ExportTariffQuery, byte[]>
{
    public Task<byte[]> Handle(ExportTariffQuery request, CancellationToken cancellationToken) =>
        admin.ExportTariffAsync(request.HmoCompanyId, request.TariffId, request.Category, request.Code, cancellationToken);
}

public sealed class ExportTariffQueryValidator : AbstractValidator<ExportTariffQuery>
{
    public ExportTariffQueryValidator()
    {
        RuleFor(x => x.HmoCompanyId).NotEmpty();
        RuleFor(x => x.TariffId).NotEmpty();
        RuleFor(x => x.Code).MaximumLength(100);
    }
}
