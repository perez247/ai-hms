using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Nurse uploads the signed certificate before submitting the record to a Doctor.</summary>
public sealed record UploadFitnessCertificateCommand(Guid CertificateId, string FileName, string ContentType, byte[] Content)
    : IRequest<FitnessCertificateDto>;

public sealed class UploadFitnessCertificateCommandHandler(IFitnessService fitness)
    : IRequestHandler<UploadFitnessCertificateCommand, FitnessCertificateDto>
{
    public async Task<FitnessCertificateDto> Handle(UploadFitnessCertificateCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.Content, writable: false);
        return await fitness.UploadCertificateAsync(request.CertificateId, request.FileName, request.ContentType, stream, cancellationToken);
    }
}

public sealed class UploadFitnessCertificateCommandValidator : AbstractValidator<UploadFitnessCertificateCommand>
{
    public UploadFitnessCertificateCommandValidator()
    {
        RuleFor(x => x.CertificateId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 10_000_000).WithMessage("Certificate must be 10 MB or smaller.");
    }
}
