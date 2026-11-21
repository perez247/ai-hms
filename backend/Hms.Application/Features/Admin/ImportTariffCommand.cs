using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

/// <summary>Imports a versioned HMO tariff from CSV bytes; the API converts IFormFile to bytes.</summary>
public sealed record ImportTariffCommand(Guid HmoCompanyId, string FileName, byte[] CsvContent, DateOnly EffectiveFrom, DateOnly? EffectiveTo)
    : IRequest<TariffImportResultDto>;

public sealed class ImportTariffCommandHandler(IAdminService admin) : IRequestHandler<ImportTariffCommand, TariffImportResultDto>
{
    public async Task<TariffImportResultDto> Handle(ImportTariffCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.CsvContent, writable: false);
        return await admin.ImportTariffAsync(request.HmoCompanyId, request.FileName, stream, request.EffectiveFrom, request.EffectiveTo, cancellationToken);
    }
}

public sealed class ImportTariffCommandValidator : AbstractValidator<ImportTariffCommand>
{
    public ImportTariffCommandValidator()
    {
        RuleFor(x => x.HmoCompanyId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CsvContent).NotEmpty().Must(x => x.Length <= 10_000_000)
            .WithMessage("Tariff file must be 10 MB or smaller.");
        RuleFor(x => x.EffectiveTo).GreaterThan(x => x.EffectiveFrom).When(x => x.EffectiveTo is not null);
    }
}
