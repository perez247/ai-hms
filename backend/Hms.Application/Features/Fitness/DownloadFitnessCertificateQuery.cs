using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

public sealed record DownloadFitnessCertificateQuery(Guid CertificateId) : IRequest<FitnessCertificateFileDto>;

public sealed class DownloadFitnessCertificateQueryHandler(IFitnessService fitness)
    : IRequestHandler<DownloadFitnessCertificateQuery, FitnessCertificateFileDto>
{
    public Task<FitnessCertificateFileDto> Handle(DownloadFitnessCertificateQuery request, CancellationToken cancellationToken) =>
        fitness.DownloadCertificateAsync(request.CertificateId, cancellationToken);
}

public sealed class DownloadFitnessCertificateQueryValidator : AbstractValidator<DownloadFitnessCertificateQuery>
{
    public DownloadFitnessCertificateQueryValidator() => RuleFor(x => x.CertificateId).NotEmpty();
}
