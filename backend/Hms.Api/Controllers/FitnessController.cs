using Hms.Application.DTOs;
using Hms.Application.Features.Fitness;
using Hms.Application.Security;
using Hms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

/// <summary>Pre-procedure clearance: nurse capture/upload/commit, doctor decision, finance payment.</summary>
[ApiController]
[Route("api/fitness")]
[Authorize]
public sealed class FitnessController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.FitnessRead)]
    public async Task<IActionResult> Search([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? hmoCompanyId, [FromQuery] string? search,
        [FromQuery] FitnessStatus? status = null, [FromQuery] FitnessPaymentType? paymentType = null,
        [FromQuery] FitnessPaymentStatus? paymentStatus = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchFitnessCertificatesQuery(from, to, hmoCompanyId, search, page, pageSize,
            status, paymentType, paymentStatus), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.FitnessCreate)]
    public async Task<ActionResult<FitnessCertificateDto>> Create(CreateFitnessCertificateRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateFitnessCertificateCommand(request), cancellationToken));

    [HttpPost("{id:guid}/certificate")]
    [Authorize(Policy = Permissions.FitnessCertificateUpload)]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<FitnessCertificateDto>> UploadCertificate(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest("Certificate file is empty.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await mediator.Send(new UploadFitnessCertificateCommand(id, file.FileName,
            file.ContentType ?? "application/octet-stream", stream.ToArray()), cancellationToken));
    }

    [HttpGet("{id:guid}/certificate")]
    [Authorize(Policy = Permissions.FitnessRead)]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        var file = await mediator.Send(new DownloadFitnessCertificateQuery(id), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/clinical")]
    [Authorize(Policy = Permissions.FitnessCertificateUpload)]
    public async Task<ActionResult<FitnessCertificateDto>> UpdateClinical(Guid id, UpdateFitnessClinicalInfoRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateFitnessClinicalInfoCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Permissions.FitnessCertificateUpload)]
    public async Task<ActionResult<FitnessCertificateDto>> Submit(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SubmitFitnessCertificateCommand(id), cancellationToken));

    [HttpPost("{id:guid}/decision")]
    [Authorize(Policy = Permissions.FitnessApprove)]
    public async Task<ActionResult<FitnessCertificateDto>> Decide(Guid id, FitnessDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DecideFitnessCertificateCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/payment")]
    [Authorize(Policy = Permissions.FitnessPayment)]
    public async Task<ActionResult<FitnessCertificateDto>> SetPayment(Guid id, FitnessPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SetFitnessPaymentCommand(id, request), cancellationToken));

    [HttpPost("hmo/{hmoCompanyId:guid}/batch-payment")]
    [Authorize(Policy = Permissions.FitnessPayment)]
    public async Task<ActionResult<FitnessCertificateDto[]>> SetHmoBatchPayment(Guid hmoCompanyId,
        FitnessBatchPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SetHmoFitnessBatchPaymentCommand(request with { HmoCompanyId = hmoCompanyId }), cancellationToken));

    [HttpPost("{id:guid}/mark-paid")]
    [Authorize(Policy = Permissions.FitnessPayment)]
    public async Task<ActionResult<FitnessCertificateDto>> MarkPaid(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new MarkFitnessCertificatePaidCommand(id), cancellationToken));

    [HttpGet("export")]
    [Authorize(Policy = Permissions.FitnessExport)]
    public async Task<IActionResult> Export([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? hmoCompanyId, [FromQuery] string? search, [FromQuery] FitnessStatus? status = null,
        [FromQuery] FitnessPaymentType? paymentType = null, [FromQuery] FitnessPaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default) =>
        File(await mediator.Send(new ExportFitnessCertificatesQuery(from, to, hmoCompanyId, search, status, paymentType, paymentStatus), cancellationToken),
            "text/csv", $"fitness-{DateTime.UtcNow:yyyyMMdd}.csv");
}
