using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record UploadPatientFileCommand(Guid PatientId, string FileName, string ContentType, byte[] Content)
    : IRequest<PatientFileDto>;

public sealed class UploadPatientFileCommandHandler(IPatientProfileService profiles)
    : IRequestHandler<UploadPatientFileCommand, PatientFileDto>
{
    public async Task<PatientFileDto> Handle(UploadPatientFileCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.Content, writable: false);
        return await profiles.UploadFileAsync(request.PatientId, request.FileName, request.ContentType, stream, cancellationToken);
    }
}

public sealed class UploadPatientFileCommandValidator : AbstractValidator<UploadPatientFileCommand>
{
    public UploadPatientFileCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty().Must(x => x.Length <= 20_000_000).WithMessage("File must be 20 MB or smaller.");
    }
}
